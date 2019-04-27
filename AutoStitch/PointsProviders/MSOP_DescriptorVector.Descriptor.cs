using System;
using System.Collections.Generic;
using CL = OpenCL.Net;
using System.Runtime.InteropServices;
using System.IO;

namespace AutoStitch.PointsProviders
{
    public partial class MSOP_DescriptorVector
    {
        public class Descriptor
        {
            class OpenCL
            {
                public static OpenCL instance = new OpenCL();
                ~OpenCL() { if (program_initialized) CL.Cl.ReleaseProgram(program); }
                CL.Program program;
                bool program_initialized = false;
                object cl_lock = new object();
                public int[] ClosestPoints(double[,] source_points, double[,]target_points)
                {
                    System.Diagnostics.Trace.Assert(source_points.GetLength(1) == target_points.GetLength(1));
                    int[] ans = new int[source_points.GetLength(0)];
                    CL.ErrorCode error;
                    byte[] source_points_byte_array = new byte[Marshal.SizeOf(typeof(double)) * source_points.Length];
                    byte[] target_points_byte_array = new byte[Marshal.SizeOf(typeof(double)) * target_points.Length];
                    byte[] dims_byte_array = new byte[Marshal.SizeOf(typeof(int)) * 3];
                    byte[] output_byte_array = new byte[Marshal.SizeOf(typeof(int)) * ans.Length];
                    int[] dims = new[] { source_points.GetLength(1), source_points.GetLength(0), target_points.GetLength(0) };
                    MyCL.memcpy(ref source_points, ref source_points_byte_array);
                    MyCL.memcpy(ref target_points, ref target_points_byte_array);
                    MyCL.memcpy(ref dims, ref dims_byte_array);
                    lock (cl_lock)
                    {
                        if (!program_initialized)
                        {
                            program_initialized = true;
                            string programPath = Path.Combine(Environment.CurrentDirectory, "../../ClosestPoints.cl");
                            if (!File.Exists(programPath)) throw new Exception("Program doesn't exist at path " + programPath);
                            string programSource = System.IO.File.ReadAllText(programPath);

                            program = CL.Cl.CreateProgramWithSource(MyCL.context, 1, new[] { programSource }, null, out error);
                            MyCL.CheckErr(error, "Cl.CreateProgramWithSource");
                            error = CL.Cl.BuildProgram(program, 1, new[] { MyCL.device }, string.Empty, null, IntPtr.Zero);
                            MyCL.CheckErr(error, "Cl.BuildProgram");
                            if (CL.Cl.GetProgramBuildInfo(program, MyCL.device, CL.ProgramBuildInfo.Status, out error).CastTo<CL.BuildStatus>() != CL.BuildStatus.Success)
                            {
                                MyCL.CheckErr(error, "Cl.GetProgramBuildInfo");
                                throw new Exception($"Cl.GetProgramBuildInfo != Success\r\n{CL.Cl.GetProgramBuildInfo(program, MyCL.device, CL.ProgramBuildInfo.Log, out error)}");
                            }
                        }
                        using (CL.Kernel kernel = CL.Cl.CreateKernel(program, "weighted_sum", out error))
                        {
                            MyCL.CheckErr(error, "Cl.CreateKernel");
                            //OpenCL memory buffer that will keep our image's byte[] data.
                            using (CL.IMem
                                source_points_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.ReadOnly, source_points_byte_array, out CL.ErrorCode err1),
                                target_points_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.ReadOnly, target_points_byte_array, out CL.ErrorCode err2),
                                dims_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.ReadOnly, dims_byte_array, out CL.ErrorCode err3),
                                output_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.WriteOnly, output_byte_array, out CL.ErrorCode err4))
                            {
                                MyCL.CheckErr(err1, "Cl.CreateBuffer source_points");
                                MyCL.CheckErr(err2, "Cl.CreateBuffer target_points");
                                MyCL.CheckErr(err3, "Cl.CreateBuffer dims");
                                MyCL.CheckErr(err4, "Cl.CreateBuffer output");
                                int intPtrSize = Marshal.SizeOf(typeof(IntPtr));
                                error =
                                    CL.Cl.SetKernelArg(kernel, 0, (IntPtr)intPtrSize, source_points_buffer) |
                                    CL.Cl.SetKernelArg(kernel, 1, (IntPtr)intPtrSize, target_points_buffer) |
                                    CL.Cl.SetKernelArg(kernel, 2, (IntPtr)intPtrSize, dims_buffer) |
                                    CL.Cl.SetKernelArg(kernel, 3, (IntPtr)intPtrSize, output_buffer);
                                MyCL.CheckErr(error, "Cl.SetKernelArg");

                                //Create a command queue, where all of the commands for execution will be added
                                using (CL.CommandQueue cmdQueue = CL.Cl.CreateCommandQueue(MyCL.context, MyCL.device, (CL.CommandQueueProperties)0, out error))
                                {
                                    MyCL.CheckErr(error, "Cl.CreateCommandQueue");
                                    CL.Event clevent;
                                    IntPtr[] workGroupSizePtr = new IntPtr[] { (IntPtr)source_points.GetLength(0) };
                                    error = CL.Cl.EnqueueNDRangeKernel(
                                        cmdQueue,
                                        kernel,
                                        1,
                                        null,//not used
                                        workGroupSizePtr, null, 0, null, out clevent);
                                    CL.Cl.ReleaseEvent(clevent);
                                    MyCL.CheckErr(error, "Cl.EnqueueNDRangeKernel");
                                    error = CL.Cl.Finish(cmdQueue);
                                    MyCL.CheckErr(error, "Cl.Finish");
                                    error = CL.Cl.EnqueueReadBuffer(cmdQueue, output_buffer, CL.Bool.True, 0, Marshal.SizeOf(typeof(byte)) * output_byte_array.Length, output_byte_array, 0, null, out clevent);
                                    CL.Cl.ReleaseEvent(clevent);
                                    MyCL.CheckErr(error, "Cl.EnqueueReadBuffer");
                                    MyCL.memcpy(ref output_byte_array, ref ans);
                                    //CL.Cl.ReleaseCommandQueue(cmdQueue);
                                }
                                //CL.Cl.ReleaseMemObject(data_buffer);
                                //CL.Cl.ReleaseMemObject(offsets_x_buffer);
                                //CL.Cl.ReleaseMemObject(offsets_y_buffer);
                                //CL.Cl.ReleaseMemObject(weights_buffer);
                                //CL.Cl.ReleaseMemObject(dims_buffer);
                                //CL.Cl.ReleaseMemObject(output_buffer);
                            }
                            //CL.Cl.ReleaseKernel(kernel);
                        }
                    }
                    return ans;
                }
            }
            public double[,] v { get; private set; }
            public Descriptor(double[,] v) { this.v = v; }
            public double difference(Descriptor descriptor)
            {
                double ans = 0;
                System.Diagnostics.Trace.Assert(v.GetLength(0) == descriptor.v.GetLength(0) && v.GetLength(1) == descriptor.v.GetLength(1));
                for (int i = 0; i < v.GetLength(0); i++)
                {
                    for (int j = 0; j < v.GetLength(1); j++)
                    {
                        ans += Math.Pow(v[i, j] - descriptor.v[i, j], 2);
                    }
                }
                return Math.Sqrt(ans);
            }
            public static List<int> try_match(List<ImagePoint<Descriptor>> source_points, List<ImagePoint<Descriptor>> target_points)
            {
                System.Diagnostics.Trace.Assert(source_points?.Count > 0);
                int vlen = source_points[0].content.v.Length;
                double[,] source_points_array = new double[source_points.Count, vlen];
                double[,] target_points_array = new double[target_points.Count, vlen];
                for(int i=0;i<source_points.Count;i++)
                {
                    System.Diagnostics.Trace.Assert(source_points[i].content.v.Length == vlen);
                    int j = 0;
                    foreach (double v in source_points[i].content.v) source_points_array[i, j++] = v;
                }
                for (int i = 0; i < target_points.Count; i++)
                {
                    System.Diagnostics.Trace.Assert(target_points[i].content.v.Length == vlen);
                    int j = 0;
                    foreach (double v in target_points[i].content.v) target_points_array[i, j++] = v;
                }
                return new List<int>(OpenCL.instance.ClosestPoints(source_points_array, target_points_array));
            }
            public bool try_match(List<ImagePoint<Descriptor>>points,out ImagePoint matched_point)
            {
                var main_descriptor = this;
                ImagePoint<Descriptor> nearst = null;
                double first_min = double.MaxValue, second_min = double.MaxValue;
                foreach (var p in points)
                {
                    double dis = main_descriptor.difference(p.content);
                    if (dis < first_min)
                    {
                        second_min = first_min;
                        first_min = dis;
                        nearst = p;
                    }
                    else if (dis < second_min) second_min = dis;
                }
                if (first_min / second_min < 0.8)
                {
                    //LogPanel.Log($"nearst feature diff = {main_descriptor.difference(nearst.content)}");
                    matched_point = nearst;
                    return true;
                }
                else
                {
                    //LogPanel.Log($"nearst feature too similar, no match!");
                    matched_point = null;
                    return false;
                }
            }
        }
    }
}
