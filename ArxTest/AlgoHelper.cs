using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;

namespace ArxTest
{
    public static class AlgoHelper
    {
        /// <summary>
        /// 倒圆角。生成两点，按左右上下序。
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="vec1"></param>
        /// <param name="vec2"></param>
        /// <param name="radius"></param>三角函数图像
        /// <returns></returns>
        public static Point2d[] Fillet(Point2d vertex,
            Vector2d vec1, Vector2d vec2, double radius)
        {
            var uvec1 = vec1.GetNormal();//获取单位向量GetNormal()
            var uvec2 = vec2.GetNormal();
            // var vecToCenter = (uvec1 + uvec2).GetNormal() * radius;
            var vecToCenterUnit = (uvec1 + uvec2).GetNormal();

            var vecToCenter = vecToCenterUnit * radius /
                Math.Sin(Math.Min(vecToCenterUnit.GetAngleTo(uvec1),
                         vecToCenterUnit.GetAngleTo(uvec2)));
            
            var projVec1 = uvec1 * uvec1.DotProduct(vecToCenter);
            var projVec2 = uvec2 * uvec2.DotProduct(vecToCenter);

            return new[] { vertex + projVec1, vertex + projVec2 }
                .OrderBy(p => p.X)
                .ThenBy(p => p.Y)
                .ToArray();
        }
    }
}
