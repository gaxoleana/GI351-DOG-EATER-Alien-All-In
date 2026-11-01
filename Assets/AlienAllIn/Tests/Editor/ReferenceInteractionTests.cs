using NUnit.Framework;
using UnityEngine;
namespace AlienAllIn.Tests
{
    public class ReferenceInteractionTests
    {
        [Test] public void MasksMatchBothReferenceShapesExactly()
        {
            int[] normal={1,3,5,3,1}, large={3,5,5,5,3};
            for(int y=-2;y<=2;y++) {
                int n=0,l=0;
                for(int x=-3;x<=3;x++) { if(SealGridRange.Contains(new Vector2Int(x,y)))n++; if(SealGridRange.Contains(new Vector2Int(x,y),true))l++; }
                Assert.That(n,Is.EqualTo(normal[y+2])); Assert.That(l,Is.EqualTo(large[y+2]));
            }
            Assert.That(SealGridRange.Contains(new Vector2Int(2,2),true),Is.False);
            Assert.That(SealGridRange.Contains(new Vector2Int(3,0)),Is.False);
        }
    }
}
