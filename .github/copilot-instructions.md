### Coding style
- Use var whenever the type can be inferred from the right side of the assignment.
- Avoid shortening variable names unless they are commonly accepted abbreviations (e.g., `i` for index). For example, avoid `consVal` and prefer `consumptionValue`.
- Avoid fully qualified type names in code; add `using` directives at the top of the file instead.

### Sidecar Specification Rule
- **Pattern:** For any file `{Name}.cs`, look for a sibling file `{Name}.spec.md`.
- **Pre-condition:** Before editing `{Name}.cs`, read `{Name}.spec.md` to understand the business logic and constraints.
- **Post-condition:** If the logic in `{Name}.cs` changes, you MUST update `{Name}.spec.md` to reflect those changes. 
- **Validation:** Ensure the spec remains the "Source of Truth" for the code’s intent.

### Clarification before complex tasks
- If a prompt is ambiguous or the task could involve significant code generation, architectural decisions, or multi-file changes, ask a single focused clarifying question before proceeding.