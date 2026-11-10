# Reference Patterns: Domain Architecture

Extracted from the reference codebase — a real-world DDD + Vertical Slice + CQRS reference project.
Stack: .NET 8/9, FastEndpoints, MediatR, Mapster, EF Core, MassTransit.

---

## 1. Where Does Business Computation Live?

**Answer: Aggregate behavior methods (Domain layer), coordinated by Application-layer handlers.**

Business logic lives in partial-class behavior files on the aggregate. The Application handler is thin: it loads aggregates via repositories, calls behavior methods, then saves. The handler does NOT contain business rules.

### Example: Submission check during `Article.Submit`

```csharp
// src/Services/Submission/Submission.Domain/Behaviors/Article.cs:71-90
public void Submit(IArticleAction<ArticleActionType> action, ArticleStateMachineFactory _stateMachineFactory)
{
    // check if all mandatory contribution areas are covered
    var contributionAreas = _actors.OfType<ArticleAuthor>()
        .SelectMany(author => author.ContributionAreas)
        .ToHashSet();

    var missingMandatoryAreas = ContributionAreaCategories.MandatoryAreas
        .Except(contributionAreas)
        .ToList();
    
    if (missingMandatoryAreas.Count > 0)
        throw new DomainException($"Cannot submit article: Missing mandatory contribution areas: {string.Join(", ", missingMandatoryAreas)}");

    SubmittedById = action.CreatedById;
    SubmittedOn = action.CreatedOn;

    SetStage(ArticleStage.Submitted, action, _stateMachineFactory);
}
```

**Layer:** `Submission.Domain` — `Behaviors/Article.cs`

The corresponding Application handler is 10 lines:

```csharp
// src/Services/Submission/Submission.Application/Features/SubmitArticle/SubmitArticleCommandHandler.cs:1-16
public class SubmitArticleCommandHandler(ArticleRepository _articleRepository, ArticleStateMachineFactory _stateMachineFactory)
    : IRequestHandler<SubmitArticleCommand, IdResponse>
{
    public async Task<IdResponse> Handle(SubmitArticleCommand command, CancellationToken ct)
    {
        var article = await _articleRepository.GetByIdOrThrowAsync(command.ArticleId, ct);
        article.Submit(command, _stateMachineFactory);
        await _articleRepository.SaveChangesAsync(ct);
        return new IdResponse(article.Id);
    }
}
```

### Example: Cross-aggregate coordination in `AcceptInvitation`

When logic spans multiple aggregates (Invitation + Reviewer + Article), it lives in the Application handler:

```csharp
// src/Services/Review/Review.Application/Features/Invitations/AcceptInvitation/AcceptInvitationCommandHandler.cs:9-29
public async Task<AcceptInvitationResponse> Handle(AcceptInvitationCommand command, CancellationToken ct)
{
    var article = await _articleRepository.GetByIdOrThrowAsync(command.ArticleId);
    var invitation = await _reviewInivtiationRepository.GetByTokenOrThrowAsync(command.Token);

    Reviewer? reviewer = default!;
    if (invitation.UserId != null)
        reviewer = await GetOrCreateReviewerByUserId(command, article, invitation.UserId.Value, ct);
    else
        reviewer = await GetOrCreateReviewerByEmail(command, article, invitation, ct);

    invitation.Accept();
    article.AssignReviewer(reviewer, command);

    await _articleRepository.SaveChangesAsync();
    return new AcceptInvitationResponse(article.Id, invitation.Id, reviewer.Id);
}
```

**Layer:** `Review.Application` — `Features/Invitations/AcceptInvitation/AcceptInvitationCommandHandler.cs`

The handler orchestrates — each aggregate still owns its own state mutation (`invitation.Accept()`, `article.AssignReviewer(...)`).

---

## 2. Do Computations Return Domain Types or Response DTOs?

**Answer: Domain behavior methods return domain entities or void. Handlers return response DTOs. Mapping (via Mapster `.Adapt<T>()`) happens inside the handler, never inside the aggregate.**

### Query handler — entity → response DTO

```csharp
// src/Services/Submission/Submission.Application/Features/GetArticle/GetArticleQueryHandler.cs:5-14
public class GetArticleQueryHandler(ArticleRepository _articleRepository)
    : IRequestHandler<GetArticleQuery, GetArticleResonse>
{
    public async Task<GetArticleResonse> Handle(GetArticleQuery command, CancellationToken ct)
    {
        var article = Guard.NotFound(
            await _articleRepository.GetFullArticleByIdAsync(command.ArticleId));

        return new GetArticleResonse(article.Adapt<ArticleDto>());
    }
}
```

The handler returns `GetArticleResonse(ArticleDto)` — a record wrapping an application-layer DTO.

```csharp
// src/Services/Submission/Submission.Application/Features/GetArticle/GetArticleQuery.cs:5-6
public record GetArticleQuery(int ArticleId) : IQuery<GetArticleResonse>;
public record GetArticleResonse(ArticleDto ArticleSummary);
```

```csharp
// src/Services/Submission/Submission.Application/Dtos/ArticleDto.cs
public record ArticleDto(
    int id, string Title, string Scope,
    ArticleType Type, ArticleStage Stage,
    JournalDto Journal, DateTime? SubmittedOn,
    PersonDto? SubmittedBy, List<ActorDto> Actors, List<AssetDto> Assets
);
```

### Command handler — returns simple id record

```csharp
// src/Services/Submission/Submission.Application/Features/CreateArticle/CreateArticleCommandHandler.cs:15
return new IdResponse(article.Id);
```

`IdResponse` is a shared building-block response type. Aggregate behavior returns the created entity (`Journal.CreateArticle(...)` returns `Article`); the handler decides what to surface in the response.

### Asset factory — returns domain entity

```csharp
// src/Services/Submission/Submission.Domain/Behaviors/Asset.cs:13-27
internal static Asset Create(Article article, AssetTypeDefinition type, int assetCount, IArticleAction action)
{
    return new Asset()
    {
        ArticleId = article.Id,
        Article = article,
        Name = AssetName.Create(type, assetCount),
        Number = AssetNumber.Create(type, assetCount),
        ...
    };
}
```

Domain factory returns a domain entity (`Asset`), not a DTO.

---

## 3. Value Objects in Aggregates

**Answer: Yes. VOs group related fields; they are persisted as EF Core `ComplexProperty` (owned types mapped to columns). Nested VOs (e.g., `File` containing `FileName` + `FileExtension`) are supported.**

### Aggregate with VOs

`Asset` aggregate uses `AssetName` and `AssetNumber` VOs instead of plain strings/ints, plus a nested `File` VO:

```csharp
// src/Services/Submission/Submission.Domain/Entities/Asset.cs:1-16
public partial class Asset : AggregateRoot
{
    public AssetName Name { get; private set; } = null!;
    public AssetNumber Number { get; private set; } = null!;
    public AssetState State { get; private set; }
    public AssetType Type { get; private set; }
    public int ArticleId { get; private set; }
    public File File { get; set; } = null!;
}
```

### VO base types

```csharp
// src/BuildingBlocks/Blocks.Domain/ValueObjects/StringValueObject.cs
public abstract class StringValueObject : IValueObject, IEquatable<StringValueObject>, IEquatable<string>
{
    public string Value { get; protected set; } = default!;
    ...
}

// src/BuildingBlocks/Blocks.Domain/ValueObjects/SingleValueObject.cs
public abstract class SingleValueObject<T> : IValueObject, IEquatable<SingleValueObject<T>>, IEquatable<T>
    where T : struct
{
    public T Value { get; protected set; } = default!;
    ...
}
```

### VO factory method

```csharp
// src/Services/Submission/Submission.Domain/ValueObjects/EmailAddress.cs:7-26
public class EmailAddress : StringValueObject
{
    [JsonConstructor]
    private EmailAddress(string value) => Value = value;

    public static EmailAddress Create(string value)
    {
        Guard.ThrowIfNullOrWhiteSpace(value);
        Guard.ThrowIfFalse(IsValidEmail(value), "Invalid email format.");
        return new EmailAddress(value.ToLower());
    }
}
```

```csharp
// src/Services/Submission/Submission.Domain/ValueObjects/AssetName.cs:6-17
public class AssetName : StringValueObject
{
    [JsonConstructor]
    private AssetName(string value) => Value = value;

    public static AssetName Create(AssetTypeDefinition assetType, int assetCount)
    {
        if (assetType.AllowsMultipleAssets)
            return new AssetName($"{assetType.Name.ToString()}_{assetCount + 1}");
        else
            return new AssetName(assetType.Name.ToString());
    }
}
```

### Persistence: `ComplexProperty` (owned types)

```csharp
// src/Services/Submission/Submission.Persistence/EntityConfigurations/AssetEntityConfiguration.cs:12-27
builder.ComplexProperty(
     e => e.Name, builder =>
     {
         builder.Property(vo => vo.Value)
             .HasColumnName(builder.Metadata.PropertyInfo!.Name)
             .HasMaxLength(MaxLength.C64).IsRequired();
     });

builder.ComplexProperty(
     e => e.Number, builder =>
     {
         builder.Property(vo => vo.Value)
             .HasColumnName(builder.Metadata.PropertyInfo!.Name)
             .IsRequired();
     });

builder.ComplexProperty(e => e.File, fileBuilder =>
{
    new FileEntityConfiguration().Configure(fileBuilder);
});
```

Nested VO (`File` contains `FileName` + `FileExtension`):

```csharp
// src/Services/Submission/Submission.Persistence/EntityConfigurations/FileEntityConfiguration.cs:9-28
builder.ComplexProperty(o => o.Extension, complexBuilder =>
{
    complexBuilder.Property(n => n.Value)
        .HasColumnName($"{builder.Metadata.ClrType.Name}_{complexBuilder.Metadata.PropertyInfo!.Name}")
        .HasMaxLength(MaxLength.C8);
});
builder.ComplexProperty(o => o.Name, complexBuilder =>
{
    complexBuilder.Property(n => n.Value)
        .HasColumnName($"{builder.Metadata.ClrType.Name}_{complexBuilder.Metadata.PropertyInfo!.Name}")
        .HasMaxLength(MaxLength.C64);
});
```

**Pattern: `ComplexProperty` maps each VO to flat columns. Column names default to the property name; can be overridden with `.HasColumnName()`.**

---

## 4. Aggregate Behavior Split

**Answer: Yes. State (properties + private backing fields) lives in the main `partial class` file (`Entities/`). Behavior (methods + factory methods) lives in a sibling `partial class` file (`Behaviors/`). Static factory methods are used for creation.**

### State file

```csharp
// src/Services/Submission/Submission.Domain/Entities/Article.cs:1-32
public partial class Article : AggregateRoot
{
    internal Article() {}

    public required string Title { get; init; }
    public ArticleType Type { get; init; }
    public ArticleStage Stage { get; set; }
    public int JournalId { get; init; }
    public required Journal Journal { get; init; } = null!;

    private readonly List<Asset> _assets = new();
    public IReadOnlyList<Asset> Assets => _assets.AsReadOnly();

    private readonly List<ArticleActor> _actors = new();
    public IReadOnlyList<ArticleActor> Actors => _actors.AsReadOnly();

    private readonly List<StageHistory> _stageHistories = new();
    public IReadOnlyList<StageHistory> StageHistories => _stageHistories.AsReadOnly();

    private readonly List<ArticleAction> _actions = new();
    public IReadOnlyList<ArticleAction> Actions => _actions.AsReadOnly();
}
```

### Behavior file (partial)

```csharp
// src/Services/Submission/Submission.Domain/Behaviors/Article.cs:1-126
public partial class Article
{
    public void SetStage(...) { ... }
    public void AssignAuthor(...) { ... }
    public void Approve(...) { ... }
    public void Reject(...) { ... }
    public void Submit(...) { ... }
    public Asset CreateAsset(...) { ... }
    public Asset GetOrCreateAsset(...) { ... }
    private void AddAction(...) { ... }
}
```

### Static factory on Journal (creates Article)

```csharp
// src/Services/Submission/Submission.Domain/Behaviors/Journal.cs:7-24
public partial class Journal
{
    public Article CreateArticle(string title, ArticleType Type, string scope, IArticleAction action)
    {
        var article = new Article
        {
            Title = title, Type = Type, Scope = scope, Journal = this,
            Stage = ArticleStage.Created,
            CreatedById = action.CreatedById, CreatedOn = action.CreatedOn
        };
        _articles.Add(article);
        article.AddDomainEvent(new ArticleCreated(article, action));
        return article;
    }

    public static Journal Create(JournalInfo journalInfo, IArticleAction action)
    {
        var journal = new Journal { Id = journalInfo.Id, Abbreviation = journalInfo.Abbreviation, Name = journalInfo.Name };
        return journal;
    }
}
```

### Static factory on Reviewer

```csharp
// src/Services/Review/Review.Domain/Reviewers/Behaviors/Reviewer.cs:10-33
public partial class Reviewer
{
    public static Reviewer Create(PersonInfo personInfo, IEnumerable<int> journalIds, IArticleAction action)
    {
        var reviewer = new Reviewer
        {
            Id = personInfo.Id,
            Email = EmailAddress.Create(personInfo.Email),
            ...
            CreatedById = action.CreatedById,
            CreatedOn = DateTime.UtcNow
        };
        reviewer.AddDomainEvent(new ReviewerCreated(reviewer, action));
        return reviewer;
    }
}
```

**Internal constructors (`internal Article() {}`, `private Asset() {}`) enforce that all creation goes through factory methods.**

---

## 5. Mapster Usage

**Answer: Mapster config classes implementing `IRegister` live in the Application layer under `Mappings/`. There are separate classes per concern (REST, gRPC). Registered via a `Blocks.Mapster` extension `AddMapsterConfigsFromAssemblyContaining<T>()`. Custom converters are registered via `ForType<,>().MapWith(...)` or `.Map(dest, src)`.**

### Config classes

```csharp
// src/Services/Submission/Submission.Application/Mappings/RestEndpointMappings.cs
public class RestEndpointMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ArticleActor, ActorDto>()
                .Include<ArticleAuthor, ActorDto>();

        config.NewConfig<Person, PersonDto>()
                .Include<Author, PersonDto>();

        config.NewConfig<IArticleAction<ArticleActionType>, ArticleAction>()
                .Map(dest => dest.TypeId, src => src.ActionType);

        config.ForType<string, EmailAddress>()
            .MapWith(src => EmailAddress.Create(src));
    }
}
```

```csharp
// src/Services/Submission/Submission.Application/Mappings/GrpcMappings.cs
public class GrpcMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<string, EmailAddress>()
            .MapWith(src => EmailAddress.Create(src));

        config.ForType<PersonInfo, Author>()
            .Map(dest => dest.UserId, src => src.Id)
            .Ignore(dest => dest.Id);
    }
}
```

### Registration

```csharp
// src/Services/Submission/Submission.Application/DependencyInjection.cs:20
services.AddMapsterConfigsFromAssemblyContaining<GrpcMappings>()
```

All `IRegister` implementations in the assembly are discovered and applied automatically.

### Usage at call site (inline `.Adapt<T>()`)

```csharp
// src/Services/Submission/Submission.Application/Features/GetArticle/GetArticleQueryHandler.cs:12
return new GetArticleResonse(article.Adapt<ArticleDto>());
```

```csharp
// src/Services/Submission/Submission.Domain/Behaviors/Article.cs:123
_actions.Add(action.Adapt<ArticleAction>());
```

Note: `.Adapt<T>()` is called directly from within domain behavior for intra-domain mapping (command → action record), as well as from Application handlers for entity → DTO projection.

---

## 6. Domain Services

**Answer: No explicit "DomainService" class concept exists. Cross-aggregate coordination lives in Application-layer handlers. However, the `ArticleAccessChecker` pattern is the closest equivalent — a focused Application-layer service class for logic that spans aggregates but isn't a handler.**

### `ArticleAccessChecker` — cross-aggregate security check

```csharp
// src/Services/Submission/Submission.Application/ArticleAccessChecker.cs
public class ArticleAccessChecker(SubmissionDbContext _dbContext) : IArticleAccessChecker
{
    public async Task<bool> HasAccessAsync(int? articleId, int? userId, IReadOnlySet<UserRoleType> roles, CancellationToken ct = default)
    {        
        if (articleId is null)
            return true; 

        if (userId is null || roles.IsNullOrEmpty()) 
            return false;
        
        if (roles.Overlaps([UserRoleType.EOF, UserRoleType.REVED]))
            return true;

        return await _dbContext.ArticleActors
            .AsNoTracking()
            .AnyAsync(e => e.ArticleId == articleId && e.Person.UserId == userId, ct);
    }
}
```

**Layer:** `Submission.Application` — lives at the root of the Application project, not in a `Features/` folder.

The same pattern appears in `Review.Application/ArticleAccessChecker.cs` and `Production.Application/ArticleAccessChecker.cs` — each service has its own access checker implementing the shared `IArticleAccessChecker` interface from `Articles.Security`.

### Registered as scoped service

```csharp
// src/Services/Submission/Submission.Application/DependencyInjection.cs:33
services.AddScoped<IArticleAccessChecker, ArticleAccessChecker>();
```

### Key distinction

- Logic that mutates a single aggregate → aggregate behavior method (Domain layer)  
- Logic that coordinates multiple aggregates → Application handler methods (private helpers inside the handler)  
- Logic that is reused across multiple handlers (e.g., access control) → Application service class (`ArticleAccessChecker` pattern)  
- **No standalone "Domain Service" with domain-layer dependencies** was found. The reference project does not use named domain service classes.

---

## Summary of Folder Conventions

| Concern | Location |
|---------|----------|
| Aggregate state | `{Service}.Domain/Entities/{Name}.cs` |
| Aggregate behavior | `{Service}.Domain/Behaviors/{Name}.cs` |
| Value objects | `{Service}.Domain/ValueObjects/` |
| Domain events | `{Service}.Domain/Behaviors/Events/` or `Entities/Events/` |
| Application handlers | `{Service}.Application/Features/{Feature}/{Name}CommandHandler.cs` |
| Application DTOs | `{Service}.Application/Dtos/` |
| Mapster configs | `{Service}.Application/Mappings/RestEndpointMappings.cs`, `GrpcMappings.cs` |
| Cross-handler services | `{Service}.Application/{Name}.cs` (root of Application project) |
| EF configs | `{Service}.Persistence/EntityConfigurations/{Name}EntityConfiguration.cs` |
