/commit for CosmicRumble — .claude/commands/commit.md
---
Analyze the staged changes and generate a semantic commit message.

Format: <type>(<scope>): <description>

Types: feat, fix, refactor, perf, test, docs
Scopes: gravity, character, ability, projectile, ui, turn, planet, manager

Examples:
  feat(gravity): add multi-planet vectoral force summation
  fix(turn): clear ability state on turn end
  refactor(ability): extract IAbility base implementation

Pre-commit checks:
  - Is Physics2D.gravity used anywhere?
  - Is velocity set directly anywhere?
  - Did the tests pass?

If clean: git commit -m "[message]"
