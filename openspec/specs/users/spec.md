# users Specification

## Purpose

Defines the Domus User as the domain identity linked to an external OIDC subject, and the rules for authenticating, explicitly provisioning, and resolving that user without granting automatic product access on login.

## Requirements

### Requirement: Domus User is linked to an external identity
The system MUST represent a Domus User with a Domus-owned identifier and a stable `identity_id` that equals the OIDC subject (`sub`) from the Identity Provider. Domus MUST NOT store user credentials. For this capability milestone, the User representation exposed by the API MUST include only `id` and `identity_id`.

#### Scenario: User identity linkage
- **WHEN** a Domus User exists for an authenticated OIDC subject
- **THEN** that User's `identity_id` MUST equal the token `sub`

### Requirement: Authentication does not create a Domus User
A successful authentication at the Identity Provider MUST NOT by itself create a Domus User or grant Domus product access. Ordinary authenticated reads MUST NOT provision users.

#### Scenario: Authenticated identity without Domus User
- **WHEN** a caller presents a valid access token whose `sub` has no corresponding Domus User
- **THEN** the system MUST NOT create a Domus User as a side effect of that request
- **AND** the system MUST deny Domus User resolution for that caller on read paths

### Requirement: Domus Users are provisioned through explicit self-serve creation
A Domus User MUST be created only through an explicit authenticated provisioning operation that associates the caller's token `sub` with a new Domus User. The operation MUST derive `identity_id` from the authenticated token subject and MUST NOT accept an arbitrary `identity_id` from the client body for this milestone.

#### Scenario: Self-serve provisioning creates a User
- **WHEN** an authenticated caller with no Domus User invokes the self-serve provisioning operation
- **THEN** the system MUST create a Domus User whose `identity_id` equals the token `sub`
- **AND** the system MUST return success with that User's Domus `id` and `identity_id`

#### Scenario: Duplicate identity rejected
- **WHEN** an authenticated caller whose `sub` is already linked to a Domus User invokes the self-serve provisioning operation
- **THEN** the system MUST reject the request without creating a second User for that identity

### Requirement: Authenticated identity resolves to at most one Domus User
Given a valid access token, the system MUST resolve the caller by looking up a Domus User whose `identity_id` matches the token `sub`. Resolution MUST yield at most one User.

#### Scenario: Successful resolution
- **WHEN** a caller presents a valid access token and a Domus User exists for that `sub`
- **THEN** the system MUST resolve that Domus User as the authenticated Domus caller

#### Scenario: No matching User
- **WHEN** a caller presents a valid access token and no Domus User exists for that `sub`
- **THEN** the system MUST treat the caller as authenticated at the IdP but not provisioned in Domus

### Requirement: Current-user endpoint exposes resolution outcomes
The system MUST expose a current-user read operation (`GET /me`) that returns the resolved Domus User when provisioned, and that distinguishes unauthenticated, unprovisioned, and provisioned outcomes.

#### Scenario: Provisioned caller
- **WHEN** a provisioned Domus User calls `GET /me` with a valid access token
- **THEN** the system MUST return HTTP 200 with that User's Domus `id` and `identity_id`

#### Scenario: Valid token without Domus User
- **WHEN** a caller with a valid access token but no Domus User calls `GET /me`
- **THEN** the system MUST respond with HTTP 403, not HTTP 401
- **AND** the system MUST NOT create a Domus User

#### Scenario: Missing or invalid token
- **WHEN** a caller invokes `GET /me` without a valid access token
- **THEN** the system MUST respond with HTTP 401

### Requirement: Self-serve provisioning endpoint
The system MUST expose an explicit provisioning operation (`POST /me`) that creates a Domus User for the authenticated caller.

#### Scenario: First-time provisioning
- **WHEN** an authenticated caller with no Domus User calls `POST /me` with a valid access token
- **THEN** the system MUST create the User and respond with HTTP 201 including `id` and `identity_id`

#### Scenario: Already provisioned
- **WHEN** an authenticated caller who already has a Domus User calls `POST /me`
- **THEN** the system MUST respond with HTTP 409 without creating another User

#### Scenario: Unauthenticated provisioning attempt
- **WHEN** a caller invokes `POST /me` without a valid access token
- **THEN** the system MUST respond with HTTP 401

### Requirement: Access tokens are validated as OIDC JWTs
Protected operations MUST validate the Bearer access token using standard OIDC/JWT mechanisms, including signature, issuer, audience, and expiration when applicable. The system MUST NOT call the Identity Provider on every authenticated request solely to validate the identity.

#### Scenario: Invalid token rejected
- **WHEN** a caller presents a missing, malformed, incorrectly signed, wrong-audience, wrong-issuer, or expired access token to a protected operation
- **THEN** the system MUST respond with HTTP 401
