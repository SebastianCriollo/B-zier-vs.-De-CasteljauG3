using System;
using System.Collections.Generic;
using System.Globalization;
using BezierCore;

// Consola independiente: no requiere paquetes ni cambia la solución del grupo.
internal static class VerificarCore
{
    private static int checks;
    private static readonly ControlPoint[] Cubic = {
        new ControlPoint(120,480), new ControlPoint(260,140),
        new ControlPoint(520,140), new ControlPoint(660,480)
    };

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++;
    }

    private static void Near(ControlPoint actual, ControlPoint expected, string message)
    {
        double tolerance = 1e-9 * Math.Max(1.0, Math.Max(Math.Abs(expected.X), Math.Abs(expected.Y)));
        Check(!double.IsNaN(actual.X) && !double.IsNaN(actual.Y) &&
              Math.Abs(actual.X - expected.X) <= tolerance &&
              Math.Abs(actual.Y - expected.Y) <= tolerance, message);
    }

    private static void Throws<T>(Action action, string parameter) where T : ArgumentException
    {
        try { action(); }
        catch (T ex) { Check(ex.ParamName == parameter, "Parámetro incorrecto: " + parameter); return; }
        throw new Exception("Se esperaba " + typeof(T).Name + " para " + parameter);
    }

    private static int Main()
    {
        try
        {
            Run();
            Console.WriteLine("OK: {0} comprobaciones.", checks);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        var bernstein = new BernsteinEvaluator();
        var casteljau = new DeCasteljauEvaluator();
        var expected = new[] { new ControlPoint(120,480), new ControlPoint(243.75,288.75),
            new ControlPoint(390,225), new ControlPoint(536.25,288.75), new ControlPoint(660,480) };
        Console.WriteLine("t;Bernstein X;Bernstein Y;Casteljau X;Casteljau Y;distancia");
        for (int k = 0; k < expected.Length; k++)
        {
            double t = k / 4.0;
            var b = bernstein.Evaluate(Cubic,t);
            var c = casteljau.Evaluate(Cubic,t);
            Near(b,expected[k],"Cúbica Bernstein"); Near(c,expected[k],"Cúbica Casteljau");
            Console.WriteLine("{0:F2};{1:F6};{2:F6};{3:F6};{4:F6};{5:E6}",t,b.X,b.Y,c.X,c.Y,ControlPoint.Distance(b,c));
        }
        var random = new Random(280704);
        double[] parameters = { 0, 1e-12, 0.01, 0.25, 0.5, 0.75, 0.99, 1-1e-12, 1 };
        for (int degree = 1; degree <= 50; degree++)
        {
            var points = new ControlPoint[degree+1];
            for (int i=0;i<points.Length;i++)
                points[i] = new ControlPoint(random.NextDouble()*2000-1000,random.NextDouble()*2000-1000);
            var original = (ControlPoint[])points.Clone();
            foreach (double t in parameters)
            {
                Near(bernstein.Evaluate(points,t),casteljau.Evaluate(points,t),"Grado " + degree);
                var steps = casteljau.EvaluateWithSteps(points,t);
                Check(steps.Count == degree+1,"Cantidad de niveles");
                for (int r=0;r<steps.Count;r++)
                {
                    Check(steps[r].Count == points.Length-r,"Ancho de nivel");
                    for (int i=0;i<steps[r].Count;i++)
                    {
                        var want = r==0 ? points[i] : (1-t)*steps[r-1][i] + t*steps[r-1][i+1];
                        Near(steps[r][i],want,"Recurrencia de interpolación");
                    }
                }
                Near(steps[degree][0],casteljau.Evaluate(points,t),"Último nivel");
            }
            var withCache = bernstein.Sample(points,17);
            bernstein.UsePrecomputedCoefficients=false;
            var withoutCache=bernstein.Sample(points,17);
            bernstein.UsePrecomputedCoefficients=true;
            var samples=casteljau.Sample(points,17);
            Check(samples.Count==17,"Muestras");
            for(int k=0;k<17;k++)
            {
                Near(withCache[k],withoutCache[k],"Precálculo");
                Near(samples[k],casteljau.Evaluate(points,k/16.0),"Muestreo uniforme");
            }
            for(int i=0;i<points.Length;i++) Near(points[i],original[i],"Entrada inmutable");
            Near(samples[0],points[0],"Extremo inicial");
            Near(samples[16],points[degree],"Extremo final");
        }
        foreach(IBezierEvaluator evaluator in new IBezierEvaluator[]{bernstein,casteljau})
        {
            Throws<ArgumentNullException>(()=>evaluator.Evaluate(null,0.5),"controlPoints");
            Throws<ArgumentNullException>(()=>evaluator.Sample(null,2),"controlPoints");
            foreach(var points in new[]{new ControlPoint[0],new[]{new ControlPoint(0,0)},
                new[]{new ControlPoint(double.NaN,0),new ControlPoint(1,1)},
                new[]{new ControlPoint(0,0),new ControlPoint(1,double.PositiveInfinity)}})
            {
                Throws<ArgumentException>(()=>evaluator.Evaluate(points,0.5),"controlPoints");
                Throws<ArgumentException>(()=>evaluator.Sample(points,2),"controlPoints");
                Throws<ArgumentException>(()=>casteljau.EvaluateWithSteps(points,0.5),"controlPoints");
            }
            foreach(double t in new[]{-0.1,1.1,double.NaN,double.NegativeInfinity,double.PositiveInfinity})
            {
                Throws<ArgumentOutOfRangeException>(()=>evaluator.Evaluate(Cubic,t),"t");
                Throws<ArgumentOutOfRangeException>(()=>casteljau.EvaluateWithSteps(Cubic,t),"t");
            }
            foreach(int count in new[]{int.MinValue,-1,0,1})
                Throws<ArgumentOutOfRangeException>(()=>evaluator.Sample(Cubic,count),"sampleCount");
            var line=new[]{new ControlPoint(-10,20),new ControlPoint(30,-20)};
            Near(evaluator.Evaluate(line,0.25),new ControlPoint(0,10),"Recta exacta");
            var same=new[]{new ControlPoint(7,-3),new ControlPoint(7,-3),new ControlPoint(7,-3)};
            Near(evaluator.Evaluate(same,0.37),same[0],"Puntos coincidentes");
            var two=evaluator.Sample(Cubic,2);
            Near(two[0],Cubic[0],"Dos muestras inicio"); Near(two[1],Cubic[3],"Dos muestras fin");
        }
        double[] row=BernsteinEvaluator.BinomialCoefficients(3);
        for(int i=0;i<4;i++) Check(Math.Abs(row[i]-new double[]{1,3,3,1}[i])<1e-14,"Coeficientes cúbicos");
        Check(BernsteinEvaluator.BinomialCoefficients(0)[0]==1,"Coeficiente grado cero");
        Throws<ArgumentOutOfRangeException>(()=>BernsteinEvaluator.BinomialCoefficients(-1),"n");
        Throws<ArgumentOutOfRangeException>(()=>BernsteinEvaluator.BinomialCoefficients(int.MaxValue),"n");
        Throws<ArgumentOutOfRangeException>(()=>BernsteinEvaluator.BinomialCoefficients(1100),"n");
    }
}
