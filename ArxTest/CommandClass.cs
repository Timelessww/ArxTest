using ArxDotNetLesson;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;

namespace ArxTest
{

    /// <summary>
    /// 参数
    /// </summary>
    public class Param
    {
        public double A { get; set; } = 3;
        public double B { get; set; } = 1;
        public double C { get; set; } = 2;
        public double D { get; set; } = 0.5;
        public double E { get; set; } = 0.5;
    }
    public class CommandClass
    {
        /// <summary>
        /// 填充到块
        /// </summary>
        [CommandMethod("test")]
        public void Test()
        {
            // 启动绘图参数输入界面。
            var main = new Main();
            if (main.ShowDialog() == true)
            {
                // 使用输入参数生成实体。
                var ents = DrawImpl(main.DataContext as Param);

                // 获取用户输入实体位置点。
                var editor = Application.DocumentManager.MdiActiveDocument.Editor;
                 var inputPoint = editor.GetPoint("选定插入图形的位置:");
                if (inputPoint.Status == PromptStatus.OK)
                {
                        ents.ToBlockDefinition("MyTest",null);
                        editor.WriteMessage("已经图形插入到块中!");
                   
                }
               
            }


        }

        /// <summary>
        /// 生成实体图形
        /// </summary>
        [CommandMethod("mytest1")]
        public void MyTest()
        {
            // 启动绘图参数输入界面。
            var main = new Main();
            if (main.ShowDialog() == true)
            {
                // 使用输入参数生成实体。
                var ents = DrawImpl(main.DataContext as Param);
                // 获取用户输入实体位置点。
                var editor = Application.DocumentManager.MdiActiveDocument.Editor;
                var inputPoint = editor.GetPoint("选定插入图形的位置:");
                if (inputPoint.Status == PromptStatus.OK)
                {
                    // 将实体通过坐标变换平移到用户指定位置。
                    ents.ForEach(e => e.TransformBy(
                        Matrix3d.Displacement(inputPoint.Value.GetAsVector())));
                    var ids = ents.ToSpace();
                    ////填充实体
                    //ids.Add(EntityHelper.CreateHatch("ANGLE", new[] { ents[0] as Polyline }));
                    //EntityHelper.MakeGroup("MYGROUP", ids);
                }

            }


        }
        /// <summary>
        ///将块插入到空间中
        /// </summary>
        [CommandMethod("Pinsert")]
        public void Insert()
        {

            DBHelper.Insert("MyTest", Matrix3d.Displacement(Vector3d.YAxis * 10),null,null);

        }
            /// <summary>
            /// 图形坐标凸度，倒角，标注
            /// </summary>
            /// <param name="param"></param>
            /// <returns></returns>

        public static Entity[] DrawImpl(Param param)

        {
            var polygonVertexes = new[]
           {
                new Point2d(0, 0),
                new Point2d(0, param.A), 
                new Point2d(-param.D, param.A),
                new Point2d(-param.C + param.E, param.B),
                new Point2d(-param.C, param.B),
                new Point2d(-param.C, 0),
            };

            var vec1 = polygonVertexes[1] - polygonVertexes[2];
            var vec2 = polygonVertexes[3] - polygonVertexes[2];
            var fillet = AlgoHelper.Fillet(polygonVertexes[2],
                vec1, vec2, 0.1);
            polygonVertexes = new[]
            {
                //new Point2d(0, 0),
                new Point2d(-0.1,0),
                new Point2d(0,0.1),
                //new Point2d(0, param.A),
                new Point2d(0, param.A-0.1),
                new Point2d(-0.1, param.A),
                fillet[1],
                fillet[0],
                new Point2d(-param.C + param.E, param.B),
                new Point2d(-param.C, param.B),
                new Point2d(-param.C, 0),
            };


            var ents = new Entity[]
              {
                EntityHelper.CreatePolygon(
                    polygonVertexes.SelectMany(p => p.ToArray()).ToArray(),
                      //new []{ Tuple.Create(0,Math.Tan(Math.PI/2/4))},
                      //new []{ Tuple.Create(2,Math.Tan(Math.PI/2/4))}

                      new []{ Tuple.Create(4, Math.Tan((Math.PI - vec2.GetAngleTo(vec1)) / 4)) }
                    ),
                new AlignedDimension(
                    Point3d.Origin,
                    Point3d.Origin + Vector3d.YAxis * param.A,
                    Point3d.Origin + Vector3d.XAxis * 0.5,
                    null, ObjectId.Null),
                new AlignedDimension(
                    Point3d.Origin + Vector3d.YAxis * param.A,
                    Point3d.Origin + Vector3d.YAxis * param.A - Vector3d.XAxis * param.D,
                    Point3d.Origin + Vector3d.YAxis * (param.A + 0.5),
                    null, ObjectId.Null),
                new AlignedDimension(
                    Point3d.Origin,
                    Point3d.Origin - Vector3d.XAxis * param.C,
                    Point3d.Origin - Vector3d.YAxis * 0.5,
                    null, ObjectId.Null),
                new AlignedDimension(
                    Point3d.Origin - Vector3d.XAxis * param.C,
                    Point3d.Origin - Vector3d.XAxis * param.C + Vector3d.YAxis * param.B,
                    Point3d.Origin - Vector3d.XAxis * (param.C + 0.5),
                    null, ObjectId.Null),
                new AlignedDimension(
                    Point3d.Origin - Vector3d.XAxis * (param.C - param.E) + Vector3d.YAxis * param.B,
                    Point3d.Origin - Vector3d.XAxis * param.C + Vector3d.YAxis * param.B,
                    Point3d.Origin + Vector3d.YAxis * (param.B + 0.5),
                    null, ObjectId.Null),
                new DBText()
                {
                    TextString = "A",
                    Position   = new Point3d(-0.15, param.A * 0.5, 0),
                },
                new DBText()
                {
                    TextString = "B",
                    Position   = new Point3d(-param.C + 0.15, param.B * 0.5, 0),
                },
                new DBText()
                {
                    TextString = "C",
                    Position   = new Point3d(-param.C * 0.5, 0.15, 0),
                },
                new DBText()
                {
                    TextString = "D",
                    Position   = new Point3d(-param.D * 0.5, param.A - 0.15, 0),
                },
                new DBText()
                {
                    TextString = "E",
                    Position   = new Point3d(-param.C + param.E * 0.5, param.B - 0.15, 0),
                },

                new Xline()
                {
                    BasePoint = new Point3d(0, param.A * 0.5, 0),
                    UnitDir = Vector3d.XAxis,
                }
              };
            ents.ForEach(e =>
             {
                 var txt = e as DBText;
                 if (txt != null)
                 {
                     txt.HorizontalMode = TextHorizontalMode.TextMid;
                     txt.VerticalMode = TextVerticalMode.TextVerticalMid;
                     txt.AlignmentPoint = txt.Position;
                 //txt.Annotationscale();
             }
             });
            return ents;

        }
    }

}
    
