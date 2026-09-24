using System;
using System.Collections.Generic;
using NUnit.Framework;
using NinetyNine.Core.Pooling;

namespace NinetyNine.Tests
{
    public class ObjectPoolTests
    {
        private sealed class Tile : IPoolable
        {
            public int Taken;
            public int Returned;
            public void OnTakenFromPool() => Taken++;
            public void OnReturnedToPool() => Returned++;
        }

        [Test]
        public void Released_instance_is_reused_instead_of_created()
        {
            var created = 0;
            var pool = new ObjectPool<Tile>(() =>
            {
                created++;
                return new Tile();
            });

            var first = pool.Get();
            pool.Release(first);
            var second = pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(created, Is.EqualTo(1));
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.EqualTo(0));
        }

        [Test]
        public void Poolable_hooks_run_on_get_and_release()
        {
            var pool = new ObjectPool<Tile>(() => new Tile());

            var tile = pool.Get();
            pool.Release(tile);
            pool.Get();

            Assert.That(tile.Taken, Is.EqualTo(2));
            Assert.That(tile.Returned, Is.EqualTo(1));
        }

        [Test]
        public void Double_release_throws()
        {
            var pool = new ObjectPool<Tile>(() => new Tile());
            var tile = pool.Get();
            pool.Release(tile);

            Assert.Throws<InvalidOperationException>(() => pool.Release(tile));
        }

        [Test]
        public void Release_beyond_capacity_destroys_the_extra_instance()
        {
            var destroyed = new List<Tile>();
            var pool = new ObjectPool<Tile>(() => new Tile(), onDestroy: destroyed.Add, maxInactive: 1);
            var a = pool.Get();
            var b = pool.Get();

            pool.Release(a);
            pool.Release(b);

            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(destroyed, Is.EqualTo(new[] { b }));
        }

        [Test]
        public void Prewarm_fills_up_to_capacity_and_clear_destroys_idle_instances()
        {
            var released = 0;
            var destroyed = 0;
            var pool = new ObjectPool<Tile>(() => new Tile(), onRelease: _ => released++, onDestroy: _ => destroyed++,
                maxInactive: 3);

            pool.Prewarm(5);
            Assert.That(pool.CountInactive, Is.EqualTo(3));
            Assert.That(released, Is.EqualTo(3), "Prewarmed instances go through onRelease so they start hidden.");

            pool.Clear();
            Assert.That(pool.CountInactive, Is.EqualTo(0));
            Assert.That(destroyed, Is.EqualTo(3));
        }
    }
}
