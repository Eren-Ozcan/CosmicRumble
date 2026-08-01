/review for CosmicRumble — .claude/commands/review.md
---
Review the specified file or recent changes with 5 lenses:

1. PHYSICS: Is Physics2D.gravity disabled? Is velocity set directly anywhere? Is AddForce used?
2. ARCHITECTURE: Is IAbility implemented? Is single responsibility respected? Are the interfaces preserved?
3. PERFORMANCE: Any FindObjectOfType inside Update? Any event leaks? Unnecessary allocations?
4. UNITY: Is the FixedUpdate/Update split correct? Are parameters SerializeField?
5. GAMEPLAY: Is TurnManager synchronization correct? Is ability state cleared?

For each problem: File.cs:line → problem → why it's wrong → how to fix it
