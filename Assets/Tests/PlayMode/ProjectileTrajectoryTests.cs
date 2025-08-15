using NUnit.Framework;
using UnityEngine;

public class ProjectileTrajectoryTests
{
    [Test]
    public void ProjectileFollowsParabolicArc()
    {
        Vector2 originalGravity = Physics2D.gravity;
        bool previousAutoSimulation = Physics2D.autoSimulation;
        Physics2D.gravity = new Vector2(0f, -9.81f);
        Physics2D.autoSimulation = false;

        var go = new GameObject("projectile");
        var rb = go.AddComponent<Rigidbody2D>();
        var projectile = go.AddComponent<ProjectileBase>();
        projectile.mass = 1f;
        projectile.gravityScale = 1f;

        rb.linearVelocity = new Vector2(10f, 10f);

        Physics2D.Simulate(1f);

        float expectedX = 10f;
        float expectedY = 10f + 0.5f * Physics2D.gravity.y * 1f * 1f;
        Assert.That(Mathf.Abs(rb.position.x - expectedX), Is.LessThan(0.5f));
        Assert.That(Mathf.Abs(rb.position.y - expectedY), Is.LessThan(0.5f));

        Object.DestroyImmediate(go);
        Physics2D.autoSimulation = previousAutoSimulation;
        Physics2D.gravity = originalGravity;
    }
}
