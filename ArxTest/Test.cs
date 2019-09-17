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

        //Matrix3d -Displacement 开事务
        //TransformBy

            //画圆弧
        [CommandMethod("Fan")]
        public static void Fan()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //定义圆弧上的三个点
                Point3d startPoint = new Point3d(100, 0, 0);
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



    }
   
}