using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ProjectileTrajectoryTests
{
    [UnityTest]
    public IEnumerator ProjectileFollowsParabolicArc()
    {
        Vector2 originalGravity = Physics2D.gravity;
        Physics2D.gravity = new Vector2(0f, -9.81f);

        var go = new GameObject("projectile");
        var rb = go.AddComponent<Rigidbody2D>();
        var projectile = go.AddComponent<ProjectileBase>();
        projectile.mass = 1f;
        projectile.gravityScale = 1f;

        rb.velocity = new Vector2(10f, 10f);

        yield return new WaitForSeconds(1f);

        float expectedX = 10f;
        float expectedY = 10f + 0.5f * Physics2D.gravity.y * 1f * 1f;
        Assert.That(Mathf.Abs(rb.position.x - expectedX), Is.LessThan(0.5f));
        Assert.That(Mathf.Abs(rb.position.y - expectedY), Is.LessThan(0.5f));

        Object.Destroy(go);
        Physics2D.gravity = originalGravity;
    }
}
