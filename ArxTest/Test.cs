using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;

using Autodesk.AutoCAD.Runtime;

using Autodesk.AutoCAD.Geometry;

using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.AutoCAD.DatabaseServices;

using Autodesk.AutoCAD.EditorInput;
using ArxDotNetLesson;
using DotNetARX;
using Autodesk.AutoCAD.Colors;
using System.Linq;

namespace ArxTest

{

    public class Class1

    {
        #region 四连杆的模拟运动  


        double theta = 0;

        const double PI = Math.PI;

        const int n = 72;

        const double delt = PI / n;//这个时候每°刷新一次

        Point3d p1 = new Point3d(0, 0, 0);

        Point3d p2, p3, p4;//注意没有初始化

        const double L1 = 100;//曲柄长度

        const double L2 = 150;//连杆长度

        const double L3 = 300;//摇杆长度

        const double L4 = 300;//机架长度

        int i = 4 * n;//转两圈就停下来

        [CommandMethod("cmd1")]

        public void Test()

        {

            if (i == 0)

            {

                i = 4 * n;

                return;

            }

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;

            //先清除图形

            using (Transaction trans = db.TransactionManager.StartTransaction())

            {

                PromptSelectionResult psr = ed.SelectAll();

                SelectionSet ss = psr.Value;

                if (ss != null)

                {

                    foreach (SelectedObject so in ss)

                    {

                        Entity ent = trans.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;

                        ent.Erase(true);

                    }

                }

                trans.Commit();

            }

            //清除完毕开始绘图

            using (Transaction trans = db.TransactionManager.StartTransaction())

            {

                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                p2 = new Point3d(L1 * Math.Cos(theta), L1 * Math.Sin(theta), 0);

                p4 = new Point3d(300, 0, 0);

                Line MyL1 = new Line(p1, p2);

                btr.AppendEntity(MyL1);

                trans.AddNewlyCreatedDBObject(MyL1, true);//L1

                Circle c1 = new Circle();

                c1.SetDatabaseDefaults();

                c1.Center = p2;

                c1.Radius = L2;

                Circle c2 = new Circle();

                c2.SetDatabaseDefaults();

                c2.Center = p4;

                c2.Radius = L3;

                Point3dCollection pointcoll = new Point3dCollection();

                c2.IntersectWith(c1, Intersect.OnBothOperands, pointcoll, 0, 0);

                p3 = pointcoll[1];

                Line MyL2 = new Line(p2, p3);

                Line MyL3 = new Line(p3, p4);

                btr.AppendEntity(MyL2);

                trans.AddNewlyCreatedDBObject(MyL2, true);//L2

                btr.AppendEntity(MyL3);

                trans.AddNewlyCreatedDBObject(MyL3, true);//L3

                trans.Commit();

            }

            ed.UpdateScreen();

            System.Threading.Thread.Sleep(50);

            theta += delt;

            i--;

            Test();

        }
        #endregion

        #region 公切线


        [CommandMethod("test1")]

        public void Test1()

        {

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;

            Transaction trans = db.TransactionManager.StartTransaction();

            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;



            Circle c = new Circle();

            Point3d a = new Point3d();

            //获取圆

            TypedValue[] tv = new TypedValue[1];

            tv.SetValue(new TypedValue(0, "CIRCLE"), 0);

            SelectionFilter sf = new SelectionFilter(tv);

            PromptSelectionResult psr = ed.GetSelection(sf);

            if (psr.Status == PromptStatus.OK)

            {

                SelectionSet ss = psr.Value;

                c = trans.GetObject(ss[0].ObjectId, OpenMode.ForRead) as Circle;

            }

            //获取点

            DBPoint dba = new DBPoint();

            tv = new TypedValue[1];

            tv.SetValue(new TypedValue(0, "POINT"), 0);

            sf = new SelectionFilter(tv);

            psr = ed.GetSelection(sf);

            if (psr.Status == PromptStatus.OK)

            {

                SelectionSet ss = psr.Value;

                dba = trans.GetObject(ss[0].ObjectId, OpenMode.ForRead) as DBPoint;

            }

            a = dba.Position;

            Point3d p = new Point3d((a.X + c.Center.X) / 2, (a.Y + c.Center.Y) / 2, 0);

            Vector3d v = p.GetVectorTo(a);

            double r = v.Length;

            if (r <= c.Radius)

            {

                ed.WriteMessage("\n点在所选圆内，无法做切线！");

                trans.Dispose();//一定要终止交易

                return;

            }

            Circle cp = new Circle();

            cp.SetDatabaseDefaults();

            cp.Radius = r;

            cp.Center = p;

            //切点

            Point3dCollection t = new Point3dCollection();

            cp.IntersectWith(c, Intersect.OnBothOperands, t, 0, 0);

            //画切线

            foreach (Point3d tt in t)

            {

                Line l = new Line(tt, a);

                btr.AppendEntity(l);

                trans.AddNewlyCreatedDBObject(l, true);

            }

            trans.Commit();

            trans.Dispose();

        }
        #endregion
        #region 通过给定点和两已知直线做垂线段


        [CommandMethod("test2")]

        public void Test2()

        {

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Transaction trans = db.TransactionManager.StartTransaction();

            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            Line l1 = new Line(new Point3d(100, 400, 0), new Point3d(200, 600, 0));

            Line l2 = new Line(new Point3d(150, 400, 0), new Point3d(300, 700, 0));

            l1.ColorIndex = 2;

            l2.ColorIndex = 2;

            Point3d p1, p2;

            p1 = l1.GetClosestPointTo(new Point3d(500, 500, 0), true);

            p2 = l2.GetClosestPointTo(p1, true);

            Line c = new Line(p1, p2)
            {
                ColorIndex = 1
            };

            btr.AppendEntity(l1);

            trans.AddNewlyCreatedDBObject(l1, true);

            btr.AppendEntity(l2);

            trans.AddNewlyCreatedDBObject(l2, true);

            btr.AppendEntity(c);

            trans.AddNewlyCreatedDBObject(c, true);

            trans.Commit();

            trans.Dispose();

        }
        #endregion


        #region 根据两点和半径绘制一段圆弧  
        [CommandMethod("ptest")]

        public void PprArc()

        {

            //准备工作

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;

            Transaction trans = db.TransactionManager.StartTransaction();

            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;



            PromptPointOptions ppo = new PromptPointOptions("");

            PromptPointResult ppr;

            Point3d temp = new Point3d();

            Point2d p1, p2;

            //获取两个点

            ppo.Message = "\n请输入第一个点：";

            do

            {

                ppr = ed.GetPoint(ppo);

                temp = ppr.Value;

                p1 = new Point2d(temp.X, temp.Y);



            } while (ppr.Status != PromptStatus.OK);



            ppo.Message = "\n请输入第二个点：";

            do

            {

                ppr = ed.GetPoint(ppo);

                temp = ppr.Value;

                p2 = new Point2d(temp.X, temp.Y);



            } while (ppr.Status != PromptStatus.OK);



            double L = p1.GetDistanceTo(p2);

            double R = 0;



            //获取半径

            do

            {//避免出现半径比弦长一半还小的情况出现

                PromptDoubleOptions pdo = new PromptDoubleOptions("\n最后，请输入半径：");

                PromptDoubleResult pdr = ed.GetDouble(pdo);

                if (pdr.Status == PromptStatus.OK)

                    R = pdr.Value;

            } while (R < L / 2);

            double H = R - Math.Sqrt(R * R - L * L / 4);



            Polyline poly = new Polyline();

            poly.AddVertexAt(0, p1, 2 * H / L, 0, 0);

            poly.AddVertexAt(1, p2, 0, 0, 0);

            btr.AppendEntity(poly);

            trans.AddNewlyCreatedDBObject(poly, true);

            trans.Commit();

            trans.Dispose();

        }
        #endregion





        #region 圆弧
        //画圆弧
        [CommandMethod("Fan")]
        public static void Fan()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                Point3d startPoint = new Point3d(100, 0, 0);
                //定义圆弧上的三个点
                Point3d pointOnArc = new Point3d(50, 25, 0);
                Point3d endPoint = new Point3d();
                //调用三点法画圆弧的扩展函数创建扇形的圆弧
                Arc arc = new Arc();
                arc.CreateArc(startPoint, pointOnArc, endPoint);
                //创建扇形的两条半径
                Line line1 = new Line(arc.Center, startPoint);
                Line line2 = new Line(arc.Center, endPoint);
                //一次性添加实体到模型空间，完成扇形的创建
                db.AddToModelSpace(line1, line2, arc);
                trans.Commit();
            }
        }


        #endregion

        #region 多段线形成圆弧
        [CommandMethod("AddPolyline")]
        public void AddPolyline()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                Point2d startPoint = Point2d.Origin;
                Point2d endPoint = new Point2d(100, 100);
                Point2d pt = new Point2d(60, 70);
                Point2d center = new Point2d(50, 50);
                //创建直线
                Polyline pline = new Polyline();
                pline.CreatePolyline(startPoint, endPoint);
                //创建矩形
                Polyline rectangle = new Polyline();
                rectangle.CreateRectangle(pt, endPoint);
                //创建正六边形
                Polyline polygon = new Polyline();
                polygon.CreatePolygon(Point2d.Origin, 6, 30);
                //创建半径为30的圆
                Polyline circle = new Polyline();
                circle.CreatePolyCircle(center, 30);
                //创建圆弧，起点角度为45度，终点角度为225度
                Polyline arc = new Polyline();
                double startAngle = 45;
                double endAngle = 225;
                arc.CreatePolyArc(center, 50, startAngle.DegreeToRadian(), endAngle.DegreeToRadian());
                //添加对象到模型空间
                db.AddToModelSpace(pline, rectangle, polygon, circle, arc);
                trans.Commit();
            }
        }
        #endregion

        #region 椭圆和样条曲线
        [CommandMethod("AddEllipse")]
        public void AddEllipse()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                Vector3d majorAxis = new Vector3d(40, 0, 0);//长轴端点
                //使用中心点、所在平面、长轴矢量和半径比例(0.5)创建一个椭圆
                Ellipse ellipse1 = new Ellipse(Point3d.Origin, Vector3d.ZAxis, majorAxis, 0.5, 0, 2 * Math.PI);
                Ellipse ellipse2 = new Ellipse();//新建一个椭圆
                //定义外接矩形的两个角点
                Point3d pt1 = new Point3d(-40, -40, 0);
                Point3d pt2 = new Point3d(40, 40, 0);
                //根据外接矩形创建椭圆
                ellipse2.CreateEllipse(pt1, pt2);
                //创建椭圆弧
                majorAxis = new Vector3d(0, 40, 0);
                Ellipse ellipseArc = new Ellipse(Point3d.Origin, Vector3d.ZAxis, majorAxis, 0.25, Math.PI, 2 * Math.PI);
                //添加实体到模型空间
                db.AddToModelSpace(ellipse1, ellipse2, ellipseArc);
                trans.Commit();
            }
        }


        [CommandMethod("AddSpline")]
        public void AddSpline()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //使用样本点直接创建4阶样条曲线
                Point3dCollection pts = new Point3dCollection
                {
                    new Point3d(0, 0, 0),
                    new Point3d(10, 30, 0),
                    new Point3d(60, 80, 0),
                    new Point3d(100, 100, 0)
                };
                Spline spline1 = new Spline(pts, 4, 0);
                //根据起点和终点为的切线方向创建样条曲线
                Vector3d startTangent = new Vector3d(5, 1, 0);
                Vector3d endTangent = new Vector3d(5, 1, 0);
                pts[1] = new Point3d(30, 10, 0);
                pts[2] = new Point3d(80, 60, 0);
                Spline spline2 = new Spline(pts, startTangent, endTangent, 4, 0);
                db.AddToModelSpace(spline1, spline2);
                trans.Commit();
            }
        }
        #endregion

        #region 文本DBText

        [CommandMethod("AddText")]
        public void AddText()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                DBText textFirst = new DBText
                {
                    Position = new Point3d(50, 50, 0),//文字位置
                    Height = 5,//文字高度 
                               //设置文字内容，特殊格式为≈、下划线和平方
                    TextString = "面积" + TextSpecialSymbol.AlmostEqual + TextSpecialSymbol.Underline + "2000" + TextSpecialSymbol.Underline + "m" + TextSpecialSymbol.Square,
                    //设置文字的水平对齐方式为居中
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    //设置文字的垂直对齐方式为居中
                    VerticalMode = TextVerticalMode.TextVerticalMid
                }; // 创建第一个单行文字
                //设置文字的对齐点
                textFirst.AlignmentPoint = textFirst.Position;
                DBText textSecond = new DBText
                {
                    Height = 5, //文字高度
                                //设置文字内容，特殊格式为角度、希腊字母和度数
                    TextString = TextSpecialSymbol.Angle + TextSpecialSymbol.Belta + "=45" + TextSpecialSymbol.Degree,
                    //设置文字的对齐方式为居中对齐
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid,
                    //设置文字的对齐点
                    AlignmentPoint = new Point3d(50, 40, 0)
                };// 创建第二个单行文字
                DBText textLast = new DBText
                {
                    Height = 5,// 文字高度
                               //设置文字的内容，特殊格式为直径和公差
                    TextString = TextSpecialSymbol.Diameter + "30的直径偏差为" + TextSpecialSymbol.Tolerance + "0.01",
                    //设置文字的对齐方式为居中对齐
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid,
                    //设置文字的对齐点
                    AlignmentPoint = new Point3d(50, 30, 0)
                };//创建第三个单行文字
                db.AddToModelSpace(textFirst, textSecond, textLast);//添加文本到模型空间
                trans.Commit();//提交事务处理
            }
        }

        [CommandMethod("AddStackText")]
        public void AddStackText()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                MText mtext = new MText
                {
                    Location = new Point3d(100, 40, 0)//位置
                };//创建多行文本对象
                //创建水平分数形式的堆叠文字
                string firstLine = TextTools.StackText(TextSpecialSymbol.Diameter + "20", "H7", "P7", StackType.HorizontalFraction, 0.5);
                //创建斜分数形式的堆叠文字
                string secondLine = TextTools.StackText(TextSpecialSymbol.Diameter + "20", "H7", "P7", StackType.ItalicFraction, 0.5);
                //创建公差形式的堆叠文字
                string lastLine = TextTools.StackText(TextSpecialSymbol.Diameter + "20", "+0.020", "-0.010", StackType.Tolerance, 0.5);
                //将前面定义的堆叠文字合并，作为多行文本的内容
                mtext.Contents = firstLine + MText.ParagraphBreak + secondLine + "\n" + lastLine;
                mtext.TextHeight = 5;//文本高度
                mtext.Width = 0;//文本宽度，设为0表示不会自动换行
                //设置多行文字的对齐方式正中
                mtext.Attachment = AttachmentPoint.MiddleCenter;
                db.AddToModelSpace(mtext);//添加文本到模型空间中
                trans.Commit();//提交事务处理
            }
        }
        #endregion

        #region 填充

        [CommandMethod("AddHatch")]
        public void AddHatch()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            //创建一个正六边形
            Polyline polygon = new Polyline();
            polygon.CreatePolygon(new Point2d(500, 200), 6, 30);
            //创建一个圆
            Circle circle = new Circle
            {
                Center = new Point3d(500, 200, 0),
                Radius = 10
            };
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //将正六边形和圆添加到模型空间中
                ObjectId polygonId = db.AddToModelSpace(polygon);
                ObjectId circleId = db.AddToModelSpace(circle);
                //创建一个ObjectId集合类对象，用于存储填充边界的ObjectId
                ObjectIdCollection ids = new ObjectIdCollection
                {
                    polygonId//将正六边形的ObjectId添加到边界集合中
                };
                Hatch hatch = new Hatch
                {
                    PatternScale = 0.5//设置填充图案的比例
                };//创建填充对象
                //创建填充图案选项板
                HatchPalletteDialog dlg = new HatchPalletteDialog();
                //显示填充图案选项板
                bool isOK = dlg.ShowDialog();
                //如果用户选择了填充图案，则设置填充图案名为所选的图案名，否则为当前图案名
                string patterName = isOK ? dlg.GetPattern() : HatchTools.CurrentPattern;
                //根据上面的填充图案名创建图案填充，类型为预定义,与边界关联
                hatch.CreateHatch(HatchPatternType.PreDefined, patterName, true);
                //为填充添加外边界（正六边形）
                hatch.AppendLoop(HatchLoopTypes.Outermost, ids);
                ids.Clear();//清空集合以添加新的边界
                ids.Add(circleId);//将圆的ObjectId添加到边界集合中
                //为填充添加内边界（圆）
                hatch.AppendLoop(HatchLoopTypes.Default, ids);
                hatch.EvaluateHatch(true);//计算并显示填充对象
                trans.Commit();//提交更改
            }
        }
        [CommandMethod("AddGradientHatch")]
        public void AddGradientHatch()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            //创建一个三角形
            Polyline triangle = new Polyline();
            triangle.CreatePolygon(new Point2d(550, 200), 3, 30);
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //将三角形添加到模型空间中
                ObjectId triangleId = db.AddToModelSpace(triangle);
                //创建一个ObjectId集合类对象，用于存储填充边界的ObjectId
                ObjectIdCollection ids = new ObjectIdCollection
                {
                    triangleId//将三角形的ObjectId添加到边界集合中
                };
                Hatch hatch = new Hatch();//创建填充对象
                //创建两个Color类变量，分别表示填充的起始颜色（红）和结束颜色（蓝）
                Color color1 = Color.FromColorIndex(ColorMethod.ByLayer, 1);
                Color color2 = Color.FromColor(System.Drawing.Color.Blue);
                //创建渐变填充，与边界无关联
                hatch.CreateGradientHatch(HatchGradientName.Cylinder, color1, color2, false);
                //为填充添加边界（三角形）
                hatch.AppendLoop(HatchLoopTypes.Default, ids);
                hatch.EvaluateHatch(true);//计算并显示填充对象
                trans.Commit();//提交更改
            }
        }

        #endregion

        #region 面域
        [CommandMethod("AddRegion")]
        public void AddRegion()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //创建一个三角形
                Polyline triangle = new Polyline();
                triangle.CreatePolygon(new Point2d(550, 200), 3, 30);
                //根据三角形创建面域
                List<Region> regions = RegionTools.CreateRegion(triangle);
                if (regions.Count == 0) return;//如果面域创建未成功，则返回
                Region region = regions[0];
                db.AddToModelSpace(region);//将创建的面域添加到数据库中
                //获取面域的质量特性
                GetAreaProp(region);
                trans.Commit();//提交更改
            }
        }

        [CommandMethod("AddComplexRegion")]
        public void AddComplexRegion()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //创建一个正六边形
                Polyline polygon = new Polyline();
                polygon.CreatePolygon(new Point2d(500, 200), 6, 30);
                //创建一个圆
                Circle circle = new Circle
                {
                    Center = new Point3d(500, 200, 0),
                    Radius = 10
                };
                //根据正六边形和圆创建面域
                List<Region> regions = RegionTools.CreateRegion(polygon, circle);
                if (regions.Count == 0) return;//如果面域创建未成功，则返回
                //使用LINQ按面积对面域进行排序
                List<Region> orderRegions = (from r in regions
                                             orderby r.Area
                                             select r).ToList();
                //对面域进行布尔操作，获取正六边形减去圆后的部分
                orderRegions[1].BooleanOperation(BooleanOperationType.BoolSubtract, orderRegions[0]);

                db.AddToModelSpace(regions[1]);//将上面操作好的面域添加到数据库中          

                trans.Commit();//提交更改
            }
        }

        private void GetAreaProp(Region region)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\n ----------------    面域   ----------------");
            ed.WriteMessage("\n面积:{0} ", region.Area);
            ed.WriteMessage("\n周长:{0} ", region.Perimeter);
            ed.WriteMessage("\n边界框上限:{0} ", region.GetExtentsHigh());
            ed.WriteMessage("\n边界框下限:{0} ", region.GetExtentsLow());
            ed.WriteMessage("\n质心: {0} ", region.GetCentroid());
            ed.WriteMessage("\n惯性矩为: {0}; {1} ", region.GetMomInertia()[0], region.GetMomInertia()[1]);
            ed.WriteMessage("\n惯性积为: {0} ", region.GetProdInertia());
            ed.WriteMessage("\n主力矩为: {0}; {1} ", region.GetPrinMoments()[0], region.GetPrinMoments()[1]);
            ed.WriteMessage("\n主方向为: {0}; {1} ", region.GetPrinAxes()[0], region.GetPrinAxes()[1]);
            ed.WriteMessage("\n旋转半径为: {0}; {1} ", region.GetRadiiGyration()[0], region.GetRadiiGyration()[1]);
        }

        #endregion
        #region 尺寸标注
        //AlignedDimension 对齐标注
        //RotatedDimension  转角标注
        //RadialDimension    半径标注
        //DiametricDimension   直径标注
        // LineAngularDimension2  角度标注
        //Point3AngularDimension  角度（3点）标注
        //ArcDimension //弧长标注
        //OrdnateDimension  坐标标注

        [CommandMethod("DimTest")]
        public void DimTest()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 创建要标注的图形
                Line line1 = new Line(new Point3d(30, 20, 0), new Point3d(120, 20, 0));
                Line line2 = new Line(new Point3d(120, 20, 0), new Point3d(120, 40, 0));
                Line line3 = new Line(new Point3d(120, 40, 0), new Point3d(90, 80, 0));
                Line line4 = new Line(new Point3d(90, 80, 0), new Point3d(30, 80, 0));
                Arc arc = new Arc(new Point3d(30, 50, 0), 30, Math.PI / 2, Math.PI * 3 / 2);
                Circle cir1 = new Circle(new Point3d(30, 50, 0), Vector3d.ZAxis, 15);
                Circle cir2 = new Circle(new Point3d(70, 50, 0), Vector3d.ZAxis, 10);
                //将图形添加到模型空间中
                db.AddToModelSpace(line1, line2, line3, line4, arc, cir1, cir2);
                //创建一个列表，用于存储标注对象
                List<Dimension> dims = new List<Dimension>();
                // 创建转角标注（水平）
                RotatedDimension dimRotated1 = new RotatedDimension
                {
                    //指定第一条尺寸界线的附着位置
                    XLine1Point = line1.StartPoint,
                    //指定第二条尺寸界线的附着位置
                    XLine2Point = line1.EndPoint,
                    //指定尺寸线的位置
                    DimLinePoint = GeTools.MidPoint(line1.StartPoint, line1.EndPoint).PolarPoint(-Math.PI / 2, 10),
                    DimensionText = "<>mm"//设置标注的文字为标注值+后缀mm
                };
                dims.Add(dimRotated1);//将水平转角标注添加到列表中
                //创建转角标注(垂直）
                RotatedDimension dimRotated2 = new RotatedDimension
                {
                    Rotation = Math.PI / 2,//转角标注角度为90度，表示垂直方向
                                           //指定两条尺寸界线的附着位置和尺寸线的位置
                    XLine1Point = line2.StartPoint,
                    XLine2Point = line2.EndPoint,
                    DimLinePoint = GeTools.MidPoint(line2.StartPoint, line2.EndPoint).PolarPoint(0, 10)
                };
                dims.Add(dimRotated2);//将垂直转角标注添加到列表中
                //创建转角标注（尺寸公差标注）
                RotatedDimension dimRotated3 = new RotatedDimension
                {
                    //指定两条尺寸界线的附着位置和尺寸线的位置
                    XLine1Point = line4.StartPoint,
                    XLine2Point = line4.EndPoint,
                    DimLinePoint = GeTools.MidPoint(line4.StartPoint, line4.EndPoint).PolarPoint(Math.PI / 2, 10),
                    //设置标注的文字为标注值+堆叠文字
                    DimensionText = TextTools.StackText("<>", "+0.026", "-0.025", StackType.Tolerance, 0.7)
                };
                dims.Add(dimRotated3);//将尺寸公差标注添加到列表中
                // 创建对齐标注
                AlignedDimension dimAligned = new AlignedDimension
                {
                    //指定两条尺寸界线的附着位置和尺寸线的位置
                    XLine1Point = line3.StartPoint,
                    XLine2Point = line3.EndPoint,
                    DimLinePoint = GeTools.MidPoint(line3.StartPoint, line3.EndPoint).PolarPoint(Math.PI / 2, 10),
                    //设置标注的文字为标注值+公差符号
                    DimensionText = "<>" + TextSpecialSymbol.Tolerance + "0.2"
                };
                dims.Add(dimAligned);//将对齐标注添加到列表中
                // 创建半径标注
                RadialDimension dimRadial = new RadialDimension
                {
                    Center = cir1.Center,//圆或圆弧的圆心
                                         //用于附着引线的圆或圆弧上的点
                    ChordPoint = cir1.Center.PolarPoint(GeTools.DegreeToRadian(30), 15),
                    LeaderLength = 10//引线长度
                };
                dims.Add(dimRadial);//将半径标注添加到列表中
                // 创建直径标注
                DiametricDimension dimDiametric = new DiametricDimension
                {
                    //圆或圆弧上第一个直径点的坐标
                    ChordPoint = cir2.Center.PolarPoint(GeTools.DegreeToRadian(45), 10),
                    //圆或圆弧上第二个直径点的坐标
                    FarChordPoint = cir2.Center.PolarPoint(GeTools.DegreeToRadian(-135), 10),
                    LeaderLength = 0//从 ChordPoint 到注解文字或折线处的长度
                };
                dims.Add(dimDiametric);//将直径标注添加到列表中
                // 创建角度标注
                Point3AngularDimension dimLineAngular = new Point3AngularDimension
                {
                    //圆或圆弧的圆心、或两尺寸界线间的共有顶点的坐标
                    CenterPoint = line2.StartPoint,
                    //指定两条尺寸界线的附着位置
                    XLine1Point = line1.StartPoint,
                    XLine2Point = line2.EndPoint,
                    //设置角度标志圆弧线上的点
                    ArcPoint = line2.StartPoint.PolarPoint(GeTools.DegreeToRadian(135), 10)
                };
                dims.Add(dimLineAngular);//将角度标注添加到列表中
                // 创建弧长标注,标注文字取为默认值
                ArcDimension dimArc = new ArcDimension(arc.Center, arc.StartPoint, arc.EndPoint, arc.Center.PolarPoint(Math.PI, arc.Radius + 10), "<>", db.Dimstyle);
                dims.Add(dimArc);//将弧长标注添加到列表中
                // 创建显示X轴值的坐标标注
                OrdinateDimension dimX = new OrdinateDimension
                {
                    UsingXAxis = true,//显示 X 轴值
                    DefiningPoint = cir2.Center,//标注点
                                                //指定引线终点，即标注文字显示的位置
                    LeaderEndPoint = cir2.Center.PolarPoint(-Math.PI / 2, 20)
                };
                dims.Add(dimX);//将坐标标注添加到列表中
                // 创建显示Y轴值的坐标标注
                OrdinateDimension dimY = new OrdinateDimension
                {
                    UsingXAxis = false,//显示Y轴                
                    DefiningPoint = cir2.Center,//标注点
                                                //指定引线终点，即标注文字显示的位置
                    LeaderEndPoint = cir2.Center.PolarPoint(0, 20)
                };
                dims.Add(dimY);//将坐标标注添加到列表中
                foreach (Dimension dim in dims)//遍历标注列表
                {
                    dim.DimensionStyle = db.Dimstyle;//设置标注样式为当前样式
                    db.AddToModelSpace(dim);//将标注添加到模型空间中

                }

                trans.Commit();//提交更改
            }
        }
        #endregion
        #region 引线与形位公差
        [CommandMethod("AddLeader")]
        public void AddLeader()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (var trans = db.TransactionManager.StartTransaction())
            {
                //创建一个在原点直径为0.219的圆。
                Circle circle = new Circle();
                circle.Center = Point3d.Origin;
                circle.Diameter = 0.219;
                //创建一个多行文本并设置其内容为4Xφd±0.005（其中d为圆的直径）
                MText txt = new MText();
                txt.Contents = "4X" + TextSpecialSymbol.Diameter + circle.Diameter + TextSpecialSymbol.Tolerance + "0.005";
                txt.Location = new Point3d(1, 1, 0);//文本位置
                txt.TextHeight = 0.2;//文本高度 
                db.AddToModelSpace(circle, txt);//将圆和文本添加到模型空间中                                
                Leader leader = new Leader();//创建一个引线对象
                //将圆上一点及文本位置作为引线的顶点
                leader.AppendVertex(circle.Center.PolarPoint(Math.PI / 3, circle.Radius));
                leader.AppendVertex(txt.Location);
                db.AddToModelSpace(leader);//将引线添加到模型空间中
                leader.Dimgap = 0.1;//设置引线的文字偏移为0.1
                leader.Dimasz = 0.1;//设置引线的箭头大小为0.1
                leader.Annotation = txt.ObjectId;//设置引线的注释对象为文本
                leader.EvaluateLeader();//计算引线及其关联注释之间的关系
                trans.Commit();//提交更改
            }
        }
        [CommandMethod("AddMLeader")]
        public void AddMLeader()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //创建3个点，分别表示引线的终点和两个头点
                Point3d ptEnd = new Point3d(90, 0, 0);
                Point3d pt1 = new Point3d(80, 20, 0);
                Point3d pt2 = new Point3d(100, 20, 0);
                MText mtext = new MText
                {
                    Contents = "多重引线示例"//文本内容
                };//新建多行文本
                MLeader mleader = new MLeader();//创建多重引线
                //为多重引线添加引线束，引线束由基线和一些单引线构成
                int leaderIndex = mleader.AddLeader();
                //在引线束中添加一单引线
                int lineIndex = mleader.AddLeaderLine(leaderIndex);
                mleader.AddFirstVertex(lineIndex, pt1); //在单引线中添加引线头点

                mleader.AddLastVertex(lineIndex, ptEnd); //在单引线中添加引线终点
                //在引线束中再添加一单引线，并只设置引线头点
                lineIndex = mleader.AddLeaderLine(leaderIndex);
                mleader.AddFirstVertex(lineIndex, pt2);
                //设置多重引线的注释为多行文本
                mleader.ContentType = ContentType.MTextContent;
                mleader.MText = mtext;
                //将多重引线添加到模型空间
                db.AddToModelSpace(mleader);
                trans.Commit();
            }
        }
        [CommandMethod("AddRoadMLeader")]
        public void AddRoadMLeader()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            //获取符号为点的箭头块的ObjectId
            ObjectId arrowId = db.GetArrowObjectId(DimArrowBlock.Dot);
            //如果当前图形中还未加入上述箭头块，则加入并获取其ObjectId
            if (arrowId == ObjectId.Null)
            {
                DimTools.ArrowBlock = DimArrowBlock.Dot;
                arrowId = db.GetArrowObjectId(DimArrowBlock.Dot);
            }
            //创建一个点列表，在其中添加4个要标注的点
            List<Point3d> pts = new List<Point3d>();
            pts.Add(new Point3d(150, 0, 0));
            pts.Add(new Point3d(150, 15, 0));
            pts.Add(new Point3d(150, 18, 0));
            pts.Add(new Point3d(150, 20, 0));
            //各标注点对应的文字
            List<string> contents = new List<string> { "道路中心线", "机动车道", "人行道", "绿化带" };
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < pts.Count; i++)//遍历标注点
                {
                    MText txt = new MText();//创建多行文本
                    txt.Contents = contents[i];//文本内容
                    MLeader mleader = new MLeader();//创建多重引线
                    //为多重引线添加引线束，引线束由基线和一些单引线构成
                    int leaderIndex = mleader.AddLeader();
                    //在引线束中添加一单引线，并设置引线头点和终点
                    int lineIndex = mleader.AddLeaderLine(leaderIndex);
                    mleader.AddFirstVertex(lineIndex, pts[i]);
                    mleader.AddLastVertex(lineIndex, pts[0].PolarPoint(Math.PI / 2, 20 + (i + 1) * 5));
                    mleader.ArrowSymbolId = arrowId;//设置单引线的箭头块ObjectId
                    //设置多重引线的注释为多行文本
                    mleader.ContentType = ContentType.MTextContent;
                    mleader.MText = txt;
                    db.AddToModelSpace(mleader);
                    mleader.ArrowSize = 1;//多重引线箭头大小
                    mleader.DoglegLength = 0;//多重引线基线长度设为0
                    //将基线连接到引线文字的下方并且绘制下划线
                    mleader.TextAttachmentType = TextAttachmentType.AttachmentBottomLine;
                }
                trans.Commit();
            }
        }
        [CommandMethod("AddTolerance")]
        public void AddTolerance()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //创建一个形位公差特征控制框
                FeatureControlFrame frame = new FeatureControlFrame();
                //形位公差的几何特征为位置
                string geometricSym = DimFormatCode.Position;
                //形位公差值为0.20，且带直径符号，包容条件为最大实体要求
                string torlerance = DimFormatCode.Diameter + "0.20" + DimFormatCode.CircleM;
                //形位公差的第一级基准符号,包容条件为最大实体要求
                string firstDatum = "A" + DimFormatCode.CircleM;
                //形位公差的第二级基准符号,包容条件为不考虑特征尺寸
                string secondDatum = "B" + DimFormatCode.CircleS;
                //形位公差的第三级基准符号,包容条件为最小实体要求
                string thirdDatum = "C" + DimFormatCode.CircleL;
                //设置公差特征控制框的内容为形位公差
                frame.CreateTolerance(geometricSym, torlerance, firstDatum, secondDatum, thirdDatum);
                frame.Location = new Point3d(1, 0.5, 0);//控制框的位置
                frame.Dimscale = 0.05;//控制框的大小
                db.AddToModelSpace(frame);//控制框添加到模型空间中
                trans.Commit();//提交更改
            }
        }
        [CommandMethod("AddCoordMLeader")]
        public void AddCoordMLeader()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            //获取符号为无的箭头块的ObjectId
            ObjectId arrowId = db.GetArrowObjectId(DimArrowBlock.None);
            //如果当前图形中还未加入上述箭头块，则加入并获取其ObjectId
            if (arrowId == ObjectId.Null)
            {
                DimTools.ArrowBlock = DimArrowBlock.None;
                arrowId = db.GetArrowObjectId(DimArrowBlock.None);
            }
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                Point3d ptCoord = new Point3d(80, 30, 0);//要标注的坐标点
                MText mtext = new MText();//新建多行文本
                //设置多行文本的内容为点的坐标值，并且分两行表示
                mtext.Contents = "X:" + ptCoord.X.ToString("0.000") + @"\PY:" + ptCoord.Y.ToString("0.000");
                mtext.LineSpacingFactor = 0.8;//多行文本的行间距
                MLeader leader = new MLeader();//创建多重引线
                //为多重引线添加引线束，引线束由基线和一些单引线构成
                int leaderIndex = leader.AddLeader();
                //在引线束中添加单引线
                int lineIndex = leader.AddLeaderLine(leaderIndex);
                //在单引线中添加引线头点（引线箭头所指向的点），位置为要进行标注的点
                leader.AddFirstVertex(lineIndex, ptCoord);
                //在单引线中添加引线终点
                leader.AddLastVertex(lineIndex, ptCoord.PolarPoint(Math.PI / 4, 10));
                //设置单引线的注释类型为多行文本
                leader.ContentType = ContentType.MTextContent;
                leader.MText = mtext;//设置单引线的注释文字
                //将多重引线添加到模型空间
                db.AddToModelSpace(leader);
                leader.ArrowSymbolId = arrowId;//设置单引线的箭头块ObjectId
                leader.DoglegLength = 0;//设置单引线的基线长度为0
                //将基线连接到引线文字的下方并且绘制下划线
                leader.TextAttachmentType = TextAttachmentType.AttachmentBottomOfTopLine;
                trans.Commit();
            }
        }
        #endregion

        #region 获取用户输入信息

        #endregion
    }

}