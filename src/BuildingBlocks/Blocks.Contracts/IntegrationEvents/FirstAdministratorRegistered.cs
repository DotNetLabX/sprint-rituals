namespace Blocks.Contracts.IntegrationEvents;

/// <summary>
/// Published by Identity after the very first User with Role="Admin" completes sign-in.
/// Carries the company domain derived from the admin's email address.
/// Consumed by Fokus to auto-seed AppSettings.CompanyDomain if it is not yet set.
/// </summary>
/// <param name="CompanyDomain">Domain part of the first admin's email (e.g. "acme.com").</param>
public record FirstAdministratorRegistered(string CompanyDomain);
