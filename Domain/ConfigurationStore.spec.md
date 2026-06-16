# ConfigurationStore specification

## Purpose
The ConfigurationStore<TSettings> type is the DB-backed configuration infrastructure for typed settings models in the Domain project.

## Core principles
- The store is intended to be an additive configuration layer for user-editable DB settings.
- The store maps each public writable property of a settings model to a configuration_point row using the property name as the logical key.
- Values are persisted in the existing configuration_point table using the JSON-backed value column.

## Supported value types for the initial implementation
The first implementation must support the following scalar property types:
- string
- bool
- bool?
- int
- int?
- decimal
- decimal?

## Read behavior
- On load, the store reads all relevant configuration_point rows in a single query and maps them by key.
- If a key does not yet exist in the DB, the property should fall back to the CLR default value for its type.
- Missing DB values must not be treated as hard errors during load.

## Save behavior
- On save, the store serializes each property value to JSON and writes it back to configuration_point.
- If a row for the key already exists, it is updated in place.
- If a row does not exist yet, a new configuration_point row is created automatically.

## Constraints
- The implementation should remain generic over TSettings and avoid hard-coded settings names.
