# Plan for the configuration UI

## Goal
Create a simple configuration settings page in Web/Web.Client/Pages that lets the user edit DB-backed configuration values from configuration_point, using the existing typed model in Domain/ConfigPointModel as the source of truth for the fields to display.

## Scope of this phase
- Add a new Blazor page in Web/Web.Client/Pages for editing configuration values.
- Add a backend controller in Web/Web/Controllers to load and save the configuration values.
- The API should validate incoming values and return HTTP 400 for invalid input, rather than accepting malformed data.
- Add a DTO in Web/Web.Client/DTOs to represent configuration rows as key/name/value entries instead of duplicating the typed model.
- The DTO should also carry enough metadata to let the UI choose the correct input control based on the property type, such as bool, decimal, and int.
- Use HeatPumpScheduleManager.razor as the UI pattern for layout, save behavior, and simple form structure.

## Proposed structure
1. UI page
   - Create a page similar to HeatPumpScheduleManager.razor.
   - Render one editable row per property exposed by ConfigPointModel.
   - Show the user-friendly display name from the model metadata, not raw property names.
   - Use the property type to choose the UI control automatically: checkbox for bool, numeric input for decimal/int, and plain text for string if needed.
   - Provide a Save action that posts the edited values back to the API.

2. API/controller
   - Add a controller under Web/Web/Controllers for configuration settings.
   - Expose GET and PUT endpoints for reading and saving the DB-backed settings.
   - The update endpoint should accept partial key/value input and preserve any existing configuration points not included in the payload.
   - The controller should operate on the generic configuration store infrastructure already introduced in the Domain project.

3. DTO design
   - Add a generic key/name/value DTO shape, for example:
     - Key: the property name used in the DB row
     - Name: the user-facing label from DisplayName metadata
     - Value: the current value to edit
     - Type: the CLR type name or a simple kind flag (bool / decimal / int / string) used by the UI to render the right input.
     - Optional: a format hint such as "decimal" or "number" if the UI needs to support fractional values more explicitly.
   - This avoids recreating the typed ConfigPointModel in every UI/API boundary and gives the page enough information to render type-aware controls.

4. Data flow
   - The page calls the controller to load configuration rows.
   - The controller maps the DB-backed values into the DTO model for the UI.
   - The page sends edited DTO entries back to the controller.
   - The controller validates the incoming DTO and rejects invalid values with HTTP 400.
   - The controller writes the values back into configuration_point using the existing typed store logic while preserving untouched settings.

## Design considerations
- The UI should be simple and generic so that adding a new property to ConfigPointModel automatically shows up in the page without changing the page logic.
- The page should not depend on hard-coded setting names beyond the reflection-based model metadata.
- The DTO should stay generic enough to support future properties without requiring repeated manual DTO changes.
- The update flow should be designed so that partial submissions only affect the keys they contain and do not reset unrelated configuration points to defaults.
- The DTO metadata should be sufficient for the UI to render controls correctly without needing to inspect the Domain model directly in the page.
- The solution should remain additive to the existing IConfiguration/ConfigModel flow; this page is for user-editable DB-backed settings only.

## Implementation steps for later work
1. Add the UI page and basic layout based on HeatPumpScheduleManager.
2. Add the configuration controller with GET/PUT endpoints.
3. Introduce the generic key/name/value DTO.
4. Wire the page to the controller and confirm the render/save flow.
5. Verify the page works with the current two properties and remains easy to extend when new typed settings are added.

## Notes
- This plan intentionally stops at the UI/controller/DTO design stage.