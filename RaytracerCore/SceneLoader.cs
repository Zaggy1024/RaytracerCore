using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using RaytracerCore.Raytracing;
using RaytracerCore.Raytracing.Cameras;
using RaytracerCore.Raytracing.Objects;
using RaytracerCore.Raytracing.Primitives;
using RaytracerCore.Vectors;

namespace RaytracerCore
{
	public class LoaderException : Exception
	{
		public readonly string Command;
		public readonly int Line;

		public LoaderException(string command, int line, Exception innerException) : base($"Error while parsing command {command} on line {line}", innerException)
		{
			Command = command;
			Line = line;
		}
	}

	public class SceneLoader
	{
		[ThreadStatic]
		private static IEnumerator<string> paramEnum;

		[ThreadStatic]
		private static MatrixStack stack;       // Transform stack
		[ThreadStatic]
		private static MatrixStack invStack;    // Inverse transforms

		private static readonly Regex lineRegex = new Regex("^\\s*(?:(\\w+)(?:\\s+([^\\s,#]+)(?:\\s*,?\\s+([^\\s,#]+))*)?)?\\s*(?:#.*)?$", RegexOptions.IgnoreCase);
		//                                                    ^-leading spaces      ^-first param                             ^-comment
		//                                                            ^-cmd name                           ^-repeated params

		private static string Next()
		{
			if (!paramEnum.MoveNext())
				throw new IndexOutOfRangeException("A parameter was missing from a command.");

			return paramEnum.Current;
		}

		private static double ParseDbl(string str)
		{
			return double.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
		}

		private static double NextDbl()
		{
			return ParseDbl(Next());
		}

		private static int ParseInt(string str)
		{
			return int.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
		}

		private static int NextInt()
		{
			return ParseInt(Next());
		}

		private static Vec4D NextVec(double w)
		{
			return new Vec4D(NextDbl(), NextDbl(), NextDbl(), w);
		}

		private static Vec4D Transf(Vec4D vec)
		{
			return stack.Peek() * vec;
		}

		private static Vec4D NextVecTransf()
		{
			return Transf(NextVec(1));
		}

		private static DoubleColor NextRGB()
		{
			return new DoubleColor(NextDbl(), NextDbl(), NextDbl());
		}

		private static bool NextBool()
		{
			switch (Next())
			{
				case "1":
				case "true":
				case "yes":
				case "y":
					return true;
			}

			return false;
		}

		private static List<string> ReadAll()
		{
			List<string> list = new List<string>();
			while (paramEnum.MoveNext())
				list.Add(paramEnum.Current);
			return list;
		}

		public static Scene FromFile(string filename)
		{
			StreamReader reader = null;

			try
			{
				reader = new StreamReader(filename);
				Scene outScene = new Scene();

				// Camera state
				Camera addCam = null;
				double imagePlane = 0;
				double dofAmount = 0;
				double focalLength = 0;
				Vec4D focalPoint = Vec4D.Zero;

				// Primitive state
				IObject obj = null;

				List<Primitive> prims = new List<Primitive>();
				bool twoSided = true;
				bool invert = false;
				DoubleColor emission = DoubleColor.Placeholder;
				DoubleColor diffuse = DoubleColor.Placeholder;
				DoubleColor specular = DoubleColor.Placeholder;
				double shininess = -1;
				DoubleColor refraction = DoubleColor.Placeholder;
				double refractionIndex = -1;

				stack = new MatrixStack();		// Transform stack
				invStack = new MatrixStack();	// Inverse transforms

				List<Vec4D> vertices = new List<Vec4D>();
				List<Vertex> verticesNormals = new List<Vertex>();

				int lineNum = 1;

				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					var lineMatched = lineRegex.Match(line);

					if (!lineMatched.Success)
						throw new Exception("Line did not match expected format.");

					// Line is blank
					if (lineMatched.Groups[1].Captures.Count > 0)
					{
						// Grab the command name from the first capture group.
						string cmd = lineMatched.Groups[1].Value.ToLowerInvariant();

						// Pull all the parameters from the matched groups after the command name.
						paramEnum = lineMatched.Groups.Values.Skip(2).SelectMany((g) => g.Captures).Select((c) => c.Value).GetEnumerator();

#if !DEBUG
						try
#endif
						{
							Vec4D pos;

							switch (cmd)
							{
								case "size":
									outScene.Width = NextInt();
									outScene.Height = NextInt();
									break;
								case "background":
									outScene.BackgroundRGB = NextRGB();
									outScene.BackgroundAlpha = NextDbl();
									break;
								case "ambient":
									outScene.AmbientRGB = Next() switch
									{
										"miss" => DoubleColor.Placeholder,
										"color" => NextRGB(),
										_ => throw new Exception($"Unknown ambient type {paramEnum.Current}."),
									};
									break;
								case "recursion":
								case "bounce":
									outScene.Recursion = NextInt();
									break;
								case "debug":
									outScene.DebugGeom = Next() switch
									{
										"geom" => true,
										"off" => false,
										_ => throw new Exception($"Unknown debug type {paramEnum.Current}."),
									};
									break;
								// Cameras:
								case "dof":
									imagePlane = NextDbl();
									dofAmount = NextDbl();

									switch (Next())
									{
										case "at":
											focalPoint = NextVecTransf();
											focalLength = 0;
											break;
										case "to":
											focalLength = NextDbl();
											focalPoint = Vec4D.Zero;
											break;
										case "camera":
											focalLength = 0;
											focalPoint = Vec4D.Zero;
											break;
										default:
											throw new Exception($"Unknown dof focal command {paramEnum.Current}.");
									}

									break;
								case "camera":
								case "frustum":
								case "orthographic":
									pos = NextVec(1);
									Vec4D lookAt = NextVec(1);
									Vec4D up = Transf(NextVec(0) + pos);
									pos = Transf(pos);
									up -= pos;

									if (cmd == "orthographic")
										addCam = new OrthoCamera(pos, lookAt, up, NextDbl());
									else
										addCam = new FrustumCamera(pos, lookAt, up, NextDbl());
									break;
								// Materials:
								case "twosided":
									twoSided = NextBool();
									break;
								case "invert":
									invert = NextBool();
									break;
								case "emission":
									emission = NextRGB();
									break;
								case "diffuse":
									diffuse = NextRGB();
									break;
								case "specular":
									specular = NextRGB();
									break;
								case "shininess":
									shininess = NextDbl();
									if (paramEnum.MoveNext())
										shininess = Math.Pow(shininess, ParseDbl(paramEnum.Current));
									break;
								case "refraction":
									if (Next() == "off")
									{
										refraction = DoubleColor.Placeholder;
										refractionIndex = -1;
									}
									else
									{
										refraction = new DoubleColor(ParseDbl(paramEnum.Current), NextDbl(), NextDbl());
										refractionIndex = NextDbl();
									}
									break;
								// Transforms:
								case "translate":
									Vec4D tVec = NextVec(0);
									stack.Transform(MatrixTransforms.Translate(tVec.X, tVec.Y, tVec.Z));
									invStack.InvTransform(MatrixTransforms.Translate(-tVec.X, -tVec.Y, -tVec.Z));
									break;
								case "scale":
									Vec4D sVec = NextVec(0);
									stack.Transform(MatrixTransforms.Scale(sVec.X, sVec.Y, sVec.Z));
									invStack.InvTransform(MatrixTransforms.Scale(1 / sVec.X, 1 / sVec.Y, 1 / sVec.Z));
									break;
								case "rotate":
									Vec4D axis = NextVec(0);
									double angle = NextDbl();
									stack.Transform(MatrixTransforms.Rotate(Consts.toRadians(angle), axis.Normalize()));
									invStack.InvTransform(MatrixTransforms.Rotate(-Consts.toRadians(angle), axis.Normalize()));
									break;
								case "pushtransform":
									stack.Push();
									invStack.Push();
									break;
								case "poptransform":
									stack.Pop();
									invStack.Pop();
									break;
								// Primitives:
								case "sphere":
									prims.Add(new Sphere(NextVec(1), NextDbl()));
									break;
								case "plane":
									prims.Add(new Plane(NextDbl(), NextVec(0)));
									break;
								case "vertex":
									vertices.Add(NextVec(1));
									break;
								case "tri":
									Vec4D p0 = vertices[NextInt()];
									Vec4D p1 = vertices[NextInt()];
									Vec4D p2 = vertices[NextInt()];
									bool mirror = false;

									if (paramEnum.MoveNext() && paramEnum.Current == "mirrored")
										mirror = true;

									Triangle tri = new Triangle(p0, p1, p2, mirror);
									prims.Add(tri);
									break;
								case "vertexnormal":
									verticesNormals.Add(new Vertex(NextVec(1), NextVec(0)));
									break;
								case "trinormal":
									Vertex v0 = verticesNormals[NextInt()];
									Vertex v1 = verticesNormals[NextInt()];
									Vertex v2 = verticesNormals[NextInt()];
									Triangle triNorm = new Triangle(v0, v1, v2);
									prims.Add(triNorm);
									break;
								// Objects
								case "cube":
									pos = NextVec(1);
									Vec4D size = NextVec(0);
									Cube cube = new Cube(pos, size);
									obj = cube;

									if (paramEnum.MoveNext())
									{
										switch (paramEnum.Current)
										{
											case "all":
												prims.AddRange(cube.GetChildren(Cube.AllSides));
												break;
											case "only":
												prims.AddRange(cube.GetChildren(ReadAll().Aggregate(Cube.NoSides, (sides, name) => sides | Cube.GetSide(name))));
												break;
											case "not":
												prims.AddRange(cube.GetChildren(ReadAll().Aggregate(Cube.AllSides, (sides, name) => sides & ~Cube.GetSide(name))));
												break;
											default:
												throw new Exception("Unknown option provided for cube construction: " + paramEnum.Current);
										}
									}

									prims.AddRange(obj.GetChildren(ObjectConsts.ImplicitInstance));
									break;
								// Instancing:
								case "instance":
									// Add instances from the previously assigned object (primitive group).
									prims.AddRange(ReadAll().SelectMany((s) => obj.GetChildren(s)));
									break;
								// Other:
								case "maxverts":
									break;
								case "maxvertnorms":
									break;
								default:
									Trace.WriteLine("Unknown command: " + cmd);
									break;
							}

							if (addCam != null)
							{
								addCam.imagePlane = imagePlane;
								addCam.dofAmount = dofAmount;

								if (focalPoint != Vec4D.Zero)
									addCam.focalLength = (focalPoint - addCam.position).Length;
								else if (focalLength != 0)
									addCam.focalLength = focalLength;
								else
									addCam.focalLength = (addCam.initLookAt - addCam.position).Length;

								outScene.Cameras.Add(addCam);
								addCam = null;
							}

							foreach (Primitive prim in prims)
							{
								prim.TwoSided = twoSided;
								prim.Invert = invert;

								if (emission != DoubleColor.Placeholder)
									prim.Emission = emission;

								if (diffuse != DoubleColor.Placeholder)
									prim.Diffuse = diffuse;

								if (specular != DoubleColor.Placeholder)
									prim.Specular = specular;
								if (shininess != -1)
									prim.Shininess = shininess;

								if (refraction != DoubleColor.Placeholder)
								{
									prim.Refraction = refraction;
									prim.RefractiveIndex = refractionIndex;
								}

								prim.Transform(stack.Peek(), invStack.Peek());

								outScene.AddPrimitive(prim);
							}

							prims.Clear();
						}
#if !DEBUG
						catch (Exception e) when (e is Exception || e is FormatException || e is OverflowException || e is IndexOutOfRangeException)
						{
							throw new LoaderException(cmd, lineNum, e);
						}
#endif
					}

					lineNum++;
				}

				return outScene;
			}
			catch (FileNotFoundException e)
			{
				
			}
			finally
			{
				reader?.Close();
			}

			return null;
		}
	}
}
