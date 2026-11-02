using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AlienAllIn.Tests
{
    public class CatFiveShotTests
    {
        [UnityTest]public IEnumerator UltimateFiresExactlyFiveBurstsThenEndsWithoutASixthHit()
        {
            yield return SceneManager.LoadSceneAsync("Main");yield return null;
            var game=Object.FindAnyObjectByType<MainRunGameplaySystems>();var loop=game.RunLoop;
            loop.StartRun();loop.Pause();game.DebugSpawnEnemy(EnemyArchetype.RoasterMelee);
            var enemy=game.Enemies[0];enemy.maxHealth=100;enemy.ResetActor();
            game.Cat.AddUltimateCharge(1);loop.Resume();Assert.That(game.ActivateUltimate(game.Cat),Is.True);loop.Pause();
            var tick=typeof(MainRunGameplaySystems).GetMethod("TickCatLasers",BindingFlags.Instance|BindingFlags.NonPublic);
            float damage=0;
            for(int i=0;i<5;i++)
            {
                float before=enemy.Health;tick.Invoke(game,new object[]{i==0?.01f:.5f});
                Assert.That(enemy.Health,Is.LessThan(before));if(i==0)damage=before-enemy.Health;
            }
            Assert.That(enemy.Health,Is.EqualTo(100-5*damage).Within(.001f));Assert.That(game.Cat.IsUltimateActive,Is.True);
            float after=enemy.Health;tick.Invoke(game,new object[]{.15f});Assert.That(game.Cat.IsUltimateActive,Is.False);
            tick.Invoke(game,new object[]{1f});Assert.That(enemy.Health,Is.EqualTo(after));
            game.Cat.AddUltimateCharge(1);loop.Resume();Assert.That(game.ActivateUltimate(game.Cat),Is.True);loop.Pause();
            tick.Invoke(game,new object[]{.01f});Assert.That(enemy.Health,Is.LessThan(after));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
