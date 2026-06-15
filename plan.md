# Implementation Plan: DB-backed Typed Configuration Store

## Analysis

### Strong points
- The existing Domain project already exposes the persistence boundary through ConfigurationPoint and HomeSystemContext, which makes the feature fit naturally in the current architecture.
- The current configuration_point table is already a key/value store with a unique key column and a string value column, so it is a good match for a generic typed wrapper.
- The project already contains a real configuration consumer pattern in ValueReaderService, which gives the implementation a concrete target and a good validation path.
- Keeping the store in the Domain project is reasonable for this small-scale solution because the current boundary between domain and infrastructure is intentionally lightweight.

### Weak points / risks
- The value column is now jsonb-based, so type conversion, serialization, and validation must still be explicit, but the parsing path can be standardized around JSON values.
- The initial version only needs to support string, bool, bool?, int, int?, decimal, and decimal? values, which keeps the first implementation small and predictable.
- The DB-backed store must behave differently from file-based ConfigModel: a missing key should not be treated as an error on read, but rather as a default value for the property type.
- The current approach of using IConfiguration directly in some services means the migration path should be gradual to avoid breaking existing consumers.

## Clarified assumptions
- Reuse the existing configuration_point table rather than introducing a new schema.
- Treat this as a DB-backed typed settings model, not as a strict required-key configuration service like file-based ConfigModel.
- Map each typed settings model to the existing key/value pattern using the model property names as logical keys.
- Use JSON as the storage format for all values, with a simple conversion path for the initial supported scalar types: string, bool, bool?, int, int?, decimal, and decimal?.
- On read, if a key does not exist in the DB yet, return the CLR default value for that property type; on the next save, the value is persisted automatically.
- Keep the implementation in the Domain project for this solution, because that is the current practical boundary for DB-facing infrastructure.

## Implementation phases
1. Define the typed configuration abstraction in the Domain project, including a generic load/save contract for settings models.
2. Implement persistence logic on top of HomeSystemContext and ConfigurationPoint using reflection-based mapping between model properties and configuration keys.
3. Define conversion rules for JSON-backed scalar values, null handling, and invalid input cases, with explicit handling for missing DB rows as default values rather than exceptions.
4. Update existing configuration consumers to use the typed store where appropriate, while preserving compatibility for current callers during the transition.
5. Add tests and verification for load, save, missing-row fallback behavior, invalid-value handling, and updates to existing rows.

## Relevant files
- Domain/ConfigurationPoint.cs
- Domain/HomeSystemContext.cs
- Domain/ServiceCollectionExtensions.cs
- ValueReaderService/Services/ConfigModel.cs
- ValueReaderService/Services/MissingConfigKeyException.cs
- Domain/ConfigurationPoint.cs (jsonb-backed value storage)

## Verification steps
1. Build the solution to confirm the new abstractions and wiring compile cleanly.
2. Add focused tests for typed load/save behavior, including missing-row fallback to defaults, missing-row creation on save, and invalid-value handling.
3. Verify that existing configuration rows are updated correctly and that adding a new property to a settings model does not require manual database changes.
