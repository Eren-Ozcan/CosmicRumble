/optimize for CosmicRumble — .claude/commands/optimize.md
---
Perform a performance and quality analysis of the specified file or system.

Steps:
1. Performance scan: FPS cost, GC allocation, object pool opportunities
2. Quality scan: SOLID violations, refactor opportunities, P0/P1/P2 prioritization
3. Merge the priorities — which should be done first?
4. Get approval
5. Apply → review the changes → test

Usage:
  /optimize                      → all of Assets/Scripts
  /optimize GravityBody.cs       → specific file
  /optimize gravity              → gravity system
  /optimize --perf-only          → performance only
  /optimize --quality-only       → code quality only

Output: Prioritized improvement list (P0/P1/P2) + estimated gain
