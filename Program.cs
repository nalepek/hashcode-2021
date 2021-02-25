using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hashcode2021
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var c in "abcdef") SubMain(c);
        }

        public static void SubMain(char letter)
        {
            var file = $"{letter}.txt";
            var input = $@"input\{file}";
            var lines = File.ReadAllLines(input);
            var line0 = lines[0].Split(" ");
            int D = int.Parse(line0[0]); // the duration of the simulation, in seconds
            int I = int.Parse(line0[1]); // the number of intersections (with IDs from 0 to I -1 )
            int S = int.Parse(line0[2]); // the number of streets
            int V = int.Parse(line0[3]); // the number of cars
            int F = int.Parse(line0[4]); // the bonus points for each car that reaches its destination before time

            //Console.WriteLine($"Duration: {D}");
            //Console.WriteLine($"Intersections: {I}");
            //Console.WriteLine($"Streets: {S}");
            //Console.WriteLine($"Cars: {V}");
            //Console.WriteLine($"Bonus: {F}");
            var streets =
                lines
                    .Skip(1)
                    .Take(S)
                    .Select(x => x.Split(" "))
                    .Select(S =>
                        new Street {
                            B = int.Parse(S[0]),
                            E = int.Parse(S[1]),
                            Name = S[2],
                            L = int.Parse(S[3])
                        })
                    .ToDictionary(k => k.Name, vv => vv);

            var cars =
                lines
                    .Skip(S + 1)
                    .Take(V)
                    .Select(x => x.Split(" "))
                    .Select(S =>
                        new Car(streets.Values.ToArray(),
                            S.Skip(1).ToArray())
                        { P = int.Parse(S[0]) })
                    .ToArray();

            //streets.All(S => { Console.WriteLine(S); return true; });
            foreach (var c in cars)
            {
                var carStreet = streets[c.Names[0]];
                carStreet.CarsAtEnd.Enqueue (c);
            }

            //cars.All(S => { Console.WriteLine(S); return true; });
            var intersections = new List<Intersection>();
            foreach (var
                inter
                in
                streets
                    .Values
                    .Select(x => x.E)
                    .Union(streets.Values.Select(xx => xx.B))
                    .Distinct()
            )
            intersections.Add(new Intersection { Id = inter });

            foreach (var inters in intersections)
            {
                inters
                    .Incoming
                    .AddRange(streets.Values.Where(S => S.E == inters.Id));
                inters
                    .Outgoing
                    .AddRange(streets.Values.Where(S => S.B == inters.Id));
            }

            var travellingCars = new List<Car>();
            var greenIntersections = new List<IntersectionSchedule>();
            for (int i = 0; i < D; i++)
            {
                foreach (var
                    trCar
                    in
                    travellingCars.Where(x => !x.IsFinito).ToArray()
                )
                {
                    trCar.TimeToEnd--;
                    if (trCar.TimeToEnd == 0)
                    {
                        streets[trCar.Names[trCar.Position]]
                            .CarsAtEnd
                            .Enqueue(trCar);
                        travellingCars.Remove (trCar);
                    }
                }

                foreach (var ins in intersections)
                {
                    var greenStreet =
                        ins.Incoming.SingleOrDefault(x => x.IsGreen);
                    if (greenStreet != null)
                    {
                        if (greenStreet.CarsAtEnd.Count > 0)
                        {
                            var dqCar = greenStreet.CarsAtEnd.Dequeue();
                            dqCar.Cost -=
                                streets[dqCar.Names[dqCar.Position]].L;
                            dqCar.Position++;
                            dqCar.TimeToEnd =
                                streets[dqCar.Names[dqCar.Position]].L;
                            travellingCars.Add (dqCar);
                        }

                        greenStreet.GreenTime--;
                    }

                    var costs =
                        ins
                            .Incoming
                            .Where(x => x.Cost > 0)
                            .Select(x => x.Cost)
                            .ToList();
                    if (costs.Count > 0)
                    {
                        var minCost = costs.Min();
                        var selectedStreet =
                            ins
                                .Incoming
                                .Where(i => i.Cost == minCost)
                                .FirstOrDefault();
                        if (greenStreet?.Name != selectedStreet?.Name)
                            if (greenStreet != null)
                                ins.Incoming.Remove(greenStreet);

                        if (selectedStreet != null)
                        {
                            selectedStreet.GreenTime = 1;
                            var dqCar = selectedStreet.CarsAtEnd.Dequeue();
                            dqCar.Cost -=
                                streets[dqCar.Names[dqCar.Position]].L;
                            dqCar.Position++;
                            dqCar.TimeToEnd =
                                streets[dqCar.Names[dqCar.Position]].L;
                            travellingCars.Add (dqCar);

                            var schedule =
                                greenIntersections
                                    .FirstOrDefault(x =>
                                        x.IntersectionId == ins.Id);
                            if (schedule == null)
                            {
                                schedule =
                                    new IntersectionSchedule {
                                        IntersectionId = ins.Id
                                    };
                                greenIntersections.Add (schedule);
                            }

                            if (
                                schedule
                                    .StreetGreenTime
                                    .ContainsKey(selectedStreet.Name)
                            )
                                schedule
                                    .StreetGreenTime[selectedStreet.Name]
                                    .GreenTime += 1; //nie moga byc naprzemiennie
                            else
                                schedule
                                    .StreetGreenTime
                                    .Add(selectedStreet.Name,
                                    new GreenTimeAndOrder {
                                        GreenTime = 1,
                                        Order = i
                                    });
                        }
                    }
                }
            }

            // RESULT
            var sb = new StringBuilder();
            int A =
                greenIntersections
                    .Select(x => x.IntersectionId)
                    .Distinct()
                    .Count(); // number of intersections with schedule
            sb.AppendLine(A.ToString());
            foreach (var gi in greenIntersections)
            {
                sb.AppendLine(gi.IntersectionId.ToString());
                sb.AppendLine(gi.StreetGreenTime.Count.ToString());
                foreach (var
                    st
                    in
                    gi.StreetGreenTime.OrderBy(x => x.Value.Order)
                )
                {
                    sb.AppendLine($"{st.Key} {st.Value.GreenTime.ToString()}");
                }
            }

            File.WriteAllText($@"output\output_{file}", sb.ToString());
        }

        public class IntersectionSchedule
        {
            public int IntersectionId;

            public Dictionary<string, GreenTimeAndOrder>
                StreetGreenTime = new Dictionary<string, GreenTimeAndOrder>();
        }

        public class GreenTimeAndOrder
        {
            public int GreenTime;

            public int Order;
        }

        public class Intersection
        {
            public int Id;

            public List<Street> Incoming = new List<Street>();

            public List<Street> Outgoing = new List<Street>();
        }

        public class Street
        {
            public bool IsGreen => GreenTime > 0;

            public int GreenTime;

            public int B; //start

            public int E; //end

            public string Name;

            public Queue<Car> CarsAtEnd = new Queue<Car>();

            public int L; //time to get from B to E

            public override string ToString()
            {
                return $"B:{B},E:{E},Name:{Name},L:{L}";
            }

            public int Cost
            {
                get
                {
                    return CarsAtEnd.FirstOrDefault()?.Cost ?? 0;
                }
            }
        }

        public class Car
        {
            public int P; // number of streets car wants to travel

            public int Position;

            public bool IsFinito => Position == P - 1;

            public string[] Names;

            public int TimeToEnd;

            public override string ToString()
            {
                return $"Cost:{Cost},P:{P},Names:{
                    String.Join(";", Names)},Position:{Position}";
            }

            public int Cost { get; set; }

            public Car(Street[] streets, string[] names)
            {
                var namesH = new HashSet<string>(names.Skip(1));
                Names = names;
                foreach (var s in streets)
                {
                    if (namesH.Contains(s.Name)) Cost += s.L;
                }
            }
        }
    }
}
