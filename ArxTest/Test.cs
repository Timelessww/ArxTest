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

        public void test()

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

            test();

        }
        #endregion

        #region 公切线


        [CommandMethod("test1")]

        public void test1()

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

        public void test2()

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

            Line c = new Line(p1, p2);

            c.ColorIndex = 1;

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

        public void pprArc()

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
                Point3dCollection pts = new Point3dCollection();
                pts.Add(new Point3d(0, 0, 0));
                pts.Add(new Point3d(10, 30, 0));
                pts.Add(new Point3d(60, 80, 0));
                pts.Add(new Point3d(100, 100, 0));
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
                DBText textFirst = new DBText(); // 创建第一个单行文字
                textFirst.Position = new Point3d(50, 50, 0);//文字位置
                textFirst.Height = 5;//文字高度 
                //设置文字内容，特殊格式为≈、下划线和平方
                textFirst.TextString = "面积" + TextSpecialSymbol.AlmostEqual + TextSpecialSymbol.Underline + "2000" + TextSpecialSymbol.Underline + "m" + TextSpecialSymbol.Square;
                //设置文字的水平对齐方式为居中
                textFirst.HorizontalMode = TextHorizontalMode.TextCenter;
                //设置文字的垂直对齐方式为居中
                textFirst.VerticalMode = TextVerticalMode.TextVerticalMid;
                //设置文字的对齐点
                textFirst.AlignmentPoint = textFirst.Position;
                DBText textSecond = new DBText();// 创建第二个单行文字
                textSecond.Height = 5; //文字高度
                //设置文字内容，特殊格式为角度、希腊字母和度数
                textSecond.TextString = TextSpecialSymbol.Angle + TextSpecialSymbol.Belta + "=45" + TextSpecialSymbol.Degree;
                //设置文字的对齐方式为居中对齐
                textSecond.HorizontalMode = TextHorizontalMode.TextCenter;
                textSecond.VerticalMode = TextVerticalMode.TextVerticalMid;
                //设置文字的对齐点
                textSecond.AlignmentPoint = new Point3d(50, 40, 0);
                DBText textLast = new DBText();//创建第三个单行文字
                textLast.Height = 5;// 文字高度
                //设置文字的内容，特殊格式为直径和公差
                textLast.TextString = TextSpecialSymbol.Diameter + "30的直径偏差为" + TextSpecialSymbol.Tolerance + "0.01";
                //设置文字的对齐方式为居中对齐
                textLast.HorizontalMode = TextHorizontalMode.TextCenter;
                textLast.VerticalMode = TextVerticalMode.TextVerticalMid;
                //设置文字的对齐点
                textLast.AlignmentPoint = new Point3d(50, 30, 0);
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
                MText mtext = new MText();//创建多行文本对象
                mtext.Location = new Point3d(100, 40, 0);//位置
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
            Circle circle = new Circle();
            circle.Center = new Point3d(500, 200, 0);
            circle.Radius = 10;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //将正六边形和圆添加到模型空间中
                ObjectId polygonId = db.AddToModelSpace(polygon);
                ObjectId circleId = db.AddToModelSpace(circle);
                //创建一个ObjectId集合类对象，用于存储填充边界的ObjectId
                ObjectIdCollection ids = new ObjectIdCollection();
                ids.Add(polygonId);//将正六边形的ObjectId添加到边界集合中
                Hatch hatch = new Hatch();//创建填充对象
                hatch.PatternScale = 0.5;//设置填充图案的比例
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
            Database db=HostApplicationServices.WorkingDatabase;
            //创建一个三角形
            Polyline triangle=new Polyline();
            triangle.CreatePolygon(new Point2d(550, 200), 3, 30);
            using (Transaction trans=db.TransactionManager.StartTransaction())
            {
                //将三角形添加到模型空间中
                ObjectId triangleId=db.AddToModelSpace(triangle);
                //创建一个ObjectId集合类对象，用于存储填充边界的ObjectId
                ObjectIdCollection ids=new ObjectIdCollection();
                ids.Add(triangleId);//将三角形的ObjectId添加到边界集合中
                Hatch hatch=new Hatch();//创建填充对象
                //创建两个Color类变量，分别表示填充的起始颜色（红）和结束颜色（蓝）
                Color color1=Color.FromColorIndex(ColorMethod.ByLayer, 1);
                Color color2=Color.FromColor(System.Drawing.Color.Blue);
                //创建渐变填充，与边界无关联
                hatch.CreateGradientHatch(HatchGradientName.Cylinder, color1, color2, false);
                //为填充添加边界（三角形）
                hatch.AppendLoop(HatchLoopTypes.Default, ids);
                hatch.EvaluateHatch(true);//计算并显示填充对象
                trans.Commit();//提交更改
            }
        }

        #endregion


    }

}