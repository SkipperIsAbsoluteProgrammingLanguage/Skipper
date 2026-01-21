using System;
using System.Globalization;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Skipper.VM.Tests;

public class NBodyTests
{
    
    private readonly ITestOutputHelper _output;

    public NBodyTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void Run_NBody_PrintsNumericRet()
    {
        const string code = """
    fn sqrt(double number) -> double {
        if (number <= 0.0) { return 0.0; }
        double guess = number;
        for (int i = 0; i < 20; i = i + 1) {
            guess = 0.5 * (guess + number / guess);
        }
        return guess;
    }

    class Body {
        double x;
        double y;
        double z;
        double vx;
        double vy;
        double vz;
        double mass;
    }

    fn makeBody(double x, double y, double z, double vx, double vy, double vz, double mass) -> Body {
        Body b = new Body();
        b.x = x; b.y = y; b.z = z;
        b.vx = vx; b.vy = vy; b.vz = vz;
        b.mass = mass;
        return b;
    }

    fn offsetMomentum(Body body, double px, double py, double pz) -> Body {
        double PI = 3.141592653589793;
        double SOLAR_MASS = 4.0 * PI * PI;
        body.vx = -px / SOLAR_MASS;
        body.vy = -py / SOLAR_MASS;
        body.vz = -pz / SOLAR_MASS;
        return body;
    }

    fn Jupiter() -> Body {
        double PI = 3.141592653589793;
        double SOLAR_MASS = 4.0 * PI * PI;
        double DAYS_PER_YEAR = 365.24;
        return makeBody(
          4.84143144246472090e+00,
          -1.16032004402742839e+00,
          -1.03622044471123109e-01,
          1.66007664274403694e-03 * DAYS_PER_YEAR,
          7.69901118419740425e-03 * DAYS_PER_YEAR,
          -6.90460016972063023e-05 * DAYS_PER_YEAR,
          9.54791938424326609e-04 * SOLAR_MASS
        );
    }

    fn Saturn() -> Body {
        double PI = 3.141592653589793;
        double SOLAR_MASS = 4.0 * PI * PI;
        double DAYS_PER_YEAR = 365.24;
        return makeBody(
          8.34336671824457987e+00,
          4.12479856412430479e+00,
          -4.03523417114321381e-01,
          -2.76742510726862411e-03 * DAYS_PER_YEAR,
          4.99852801234917238e-03 * DAYS_PER_YEAR,
          2.30417297573763929e-05 * DAYS_PER_YEAR,
          2.85885980666130812e-04 * SOLAR_MASS
        );
    }

    fn Uranus() -> Body {
        double PI = 3.141592653589793;
        double SOLAR_MASS = 4.0 * PI * PI;
        double DAYS_PER_YEAR = 365.24;
        return makeBody(
          1.28943695621391310e+01,
          -1.51111514016986312e+01,
          -2.23307578892655734e-01,
          2.96460137564761618e-03 * DAYS_PER_YEAR,
          2.37847173959480950e-03 * DAYS_PER_YEAR,
          -2.96589568540237556e-05 * DAYS_PER_YEAR,
          4.36624404335156298e-05 * SOLAR_MASS
        );
    }

    fn Neptune() -> Body {
        double PI = 3.141592653589793;
        double SOLAR_MASS = 4.0 * PI * PI;
        double DAYS_PER_YEAR = 365.24;
        return makeBody(
          1.53796971148509165e+01,
          -2.59193146099879641e+01,
          1.79258772950371181e-01,
          2.68067772490389322e-03 * DAYS_PER_YEAR,
          1.62824170038242295e-03 * DAYS_PER_YEAR,
          -9.51592254519715870e-05 * DAYS_PER_YEAR,
          5.15138902046611451e-05 * SOLAR_MASS
        );
    }

    fn Sun() -> Body {
        double PI = 3.141592653589793;
        double SOLAR_MASS = 4.0 * PI * PI;
        return makeBody(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, SOLAR_MASS);
    }

    fn NBodySystem(Body[] bodies, int size) {
        double px = 0.0;
        double py = 0.0;
        double pz = 0.0;

        for (int i = 0; i < size; i++) {
            Body b = bodies[i];
            double m = b.mass;
            px += b.vx * m;
            py += b.vy * m;
            pz += b.vz * m;
        }

        offsetMomentum(bodies[0], px, py, pz);
    }

    fn advance(Body[] bodies, int size, double dt) {
        double dx; double dy; double dz;
        double distance;
        double mag;
        
        for (int i = 0; i < size; i++) {
            Body bi = bodies[i];
            for (int j = i + 1; j < size; j++) {
                Body bj = bodies[j];
                dx = bi.x - bj.x;
                dy = bi.y - bj.y;
                dz = bi.z - bj.z;

                distance = sqrt(dx * dx + dy * dy + dz * dz);
                mag = dt / (distance * distance * distance);

                bi.vx -= dx * bj.mass * mag;
                bi.vy -= dy * bj.mass * mag;
                bi.vz -= dz * bj.mass * mag;

                bj.vx += dx * bi.mass * mag;
                bj.vy += dy * bi.mass * mag;
                bj.vz += dz * bi.mass * mag;
            }
        }

        for (int i = 0; i < size; i++) {
            Body b = bodies[i];
            b.x += dt * b.vx;
            b.y += dt * b.vy;
            b.z += dt * b.vz;
        }
    }

    fn energy(Body[] bodies, int size) -> double {
        double dx; double dy; double dz;
        double distance;
        double e = 0.0;

        for (int i = 0; i < size; i++) {
            Body bi = bodies[i];

            e += 0.5 * bi.mass *
                ( bi.vx * bi.vx
                + bi.vy * bi.vy
                + bi.vz * bi.vz );

            for (int j = i + 1; j < size; j++) {
                Body bj = bodies[j];
                dx = bi.x - bj.x;
                dy = bi.y - bj.y;
                dz = bi.z - bj.z;
                
                distance = sqrt(dx * dx + dy * dy + dz * dz);
                e -= (bi.mass * bj.mass) / distance;
            }
        }
        
        return e;
    }

    fn createSystem() -> Body[] {
        Body[] bodies = new Body[5];
        bodies[0] = Sun();
        bodies[1] = Jupiter();
        bodies[2] = Saturn();
        bodies[3] = Uranus();
        bodies[4] = Neptune();
        return bodies;
    }

    fn main() -> int {
        double ret = 0.0;
        int size = 5;

        for (int n = 3; n <= 24; n *= 2) {
            Body[] bodies = createSystem();
            NBodySystem(bodies, size);
            int max = n * 100;

            ret += energy(bodies, size);
            for (int i = 0; i < max; i++) {
                advance(bodies, size, 0.01);
            }
            ret += energy(bodies, size);
        }

        double expected = -1.3524862408537381;
        double diff = ret - expected;
        if (diff < 0.0) {diff = 0.0 - diff; }

        if (diff > 1e-12) {
            println("ERROR: bad result: expected " + expected + " but got " + ret);
            return 1;
        }
        
        println("Expected: " + expected);
        println("Ret: " + ret);
        println("Diff: " + diff);
            
        return 0;
    }
    """;

        var output = TestsHelpers.CaptureOutput(() => { TestsHelpers.Run(code); });
        _output.WriteLine("=== VM output ===");
        _output.WriteLine(output);
        _output.WriteLine("=== End VM output ===");
        
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var retLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("Ret: "));
        Assert.NotNull(retLine);

        var retText = retLine!.Substring(retLine.IndexOf("Ret: ", StringComparison.Ordinal) + "Ret: ".Length).Trim();
        var parsed = double.TryParse(retText, NumberStyles.Float | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var ret);
        Assert.True(parsed, $"Не удалось распарсить значение Ret: '{retText}'");
        Assert.False(double.IsNaN(ret), "Ret == NaN (ошибка: результат вычисления некорректен)");
    }
}