# Copilot Instructions

## Project Guidelines
- User prefers the Handle<Domain>() naming pattern for private methods that manage specific aspects of gameplay, regardless of whether they're called from Update or other callbacks (e.g., HandleMovement, HandleInteractions, HandleCollectables). This provides consistent, uniform naming across the codebase.

## Naming Conventions
- SerializeFields should be named using PascalCase like public properties (not _privateField convention). 
- Private fields use _privateField, constants use CONSTANT_NAMING, and parameters/local variables use simpleCamelCase.

## Unity Guidelines
- Unity USS does not support viewport units (vw, vh); use supported units like px or % instead.
- Method ordering in classes should follow this pattern: (1) Unity lifecycle messages in lifecycle order (Awake, OnEnable, Start, OnDisable, Update, LateUpdate, FixedUpdate, OnDestroy, etc.) - non-editor only, (2) Private/public methods in stepdown rule order (top-down execution flow - order they're first called), (3) Editor-only Unity messages (OnDrawGizmos, OnDrawGizmosSelected, etc.) at the end.

## Timer Management
- All timers should always decrement to zero, and for consistency, use the extension method DecrementTimer. The correct usage is: _timer.DecrementTimer();

## AI Behavior Management
- 'IsDisabled' is only a designer/editor toggle; runtime behavior activation is controlled each frame by `IAiBehaviourExtensions.UpdateActiveAiBehaviour` via `IAiBehaviour.Enable` based on `CanAct` and `Priority`.