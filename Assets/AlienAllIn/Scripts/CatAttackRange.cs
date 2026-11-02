using UnityEngine;
namespace AlienAllIn
{
    public static class CatAttackRange
    {
        public static bool Contains(Vector2Int offset)
        {
            int x=Mathf.Abs(offset.x), y=Mathf.Abs(offset.y);
            return x<=2 && y<=2 && !(x==2 && y==2);
        }

        public static Vector3 BoardEdge(BoardGrid board, Vector3 origin, Vector2 direction)
        {
            Vector3 local=board.transform.InverseTransformPoint(origin);
            Vector2Int min=board.ActiveMinCell;
            float left=min.x*board.cellSize, bottom=min.y*board.cellSize;
            float right=(min.x+board.ActiveWidth)*board.cellSize, top=(min.y+board.ActiveHeight)*board.cellSize;
            local.x=Mathf.Clamp(local.x,left,right); local.y=Mathf.Clamp(local.y,bottom,top);
            float distance=float.PositiveInfinity;
            if(Mathf.Abs(direction.x)>.00001f) distance=Mathf.Min(distance,((direction.x>0?right:left)-local.x)/direction.x);
            if(Mathf.Abs(direction.y)>.00001f) distance=Mathf.Min(distance,((direction.y>0?top:bottom)-local.y)/direction.y);
            if(float.IsInfinity(distance)) return origin;
            return board.transform.TransformPoint(local+new Vector3(direction.x,direction.y,0)*Mathf.Max(0,distance));
        }
    }
}
