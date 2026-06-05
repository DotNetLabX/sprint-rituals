---
name: create-grpc-contract
description: Creates a gRPC code-first contract, server implementation, and client registration. Use when adding service-to-service synchronous communication.
---

# Create gRPC Contract

Creates a complete gRPC code-first contract with server and client. No .proto files — pure C# with `protobuf-net.Grpc`.

## Steps

1. **Create the contract** — follow `workflows/Contract.md`
2. **Create the server implementation** — follow `workflows/Server.md`
3. **Register the client** in consuming services — follow `workflows/Client.md`
4. **Verify the build:** `dotnet build`

## Arguments

Pass the service contract name: `/create-grpc-contract OrderQueryService`

## Existing Contracts

| Contract | Service | Port | Location |
|----------|---------|------|----------|
| (check `src/BuildingBlocks/{ProjectName}.Grpc.Contracts/`) | | | |

## Port Conventions

Check the root CLAUDE.md Port Convention section for assigned service indices and derived ports.
