using NUnit.Framework;
using UnityEngine;
namespace AlienAllIn.Tests
{
    public class CatCombatTests
    {
        [Test] public void NormalMaskMatchesReferenceRows()
        {
            int[] expected={3,5,5,5,3};
            for(int y=-2;y<=2;y++){int count=0;for(int x=-3;x<=3;x++)if(CatAttackRange.Contains(new Vector2Int(x,y)))count++;Assert.That(count,Is.EqualTo(expected[y+2]));}
            Assert.That(CatAttackRange.Contains(new Vector2Int(0,3)),Is.False);
        }
        [TestCase(5)][TestCase(7)][TestCase(10)]
        public void EightLaserEndpointsReachActiveBoundariesOnTransformedBoard(int size)
        {
            var go=new GameObject("Board fixture");
            try {
                var board=go.AddComponent<BoardGrid>();board.SetActiveSize(size,size);
                go.transform.position=new Vector3(3,-4);go.transform.rotation=Quaternion.Euler(0,0,17);go.transform.localScale=new Vector3(1.3f,.8f,1);
                var min=board.ActiveMinCell;Vector3 origin=board.CellCenter(min+new Vector2Int(1,1));
                for(int i=0;i<8;i++)
                {
                    Vector2 direction=new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4));
                    Vector3 end=go.transform.InverseTransformPoint(CatAttackRange.BoardEdge(board,origin,direction));
                    float left=min.x*board.cellSize,right=(min.x+size)*board.cellSize,bottom=min.y*board.cellSize,top=(min.y+size)*board.cellSize;
                    Assert.That(end.x,Is.InRange(left-.001f,right+.001f));Assert.That(end.y,Is.InRange(bottom-.001f,top+.001f));
                    Assert.That(Mathf.Min(Mathf.Abs(end.x-left),Mathf.Abs(end.x-right),Mathf.Abs(end.y-bottom),Mathf.Abs(end.y-top)),Is.LessThan(.001f));
                }
            } finally {Object.DestroyImmediate(go);}
        }
    }
}
