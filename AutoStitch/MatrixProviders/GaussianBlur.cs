using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OpenCL.Net.Extensions;
using CL = OpenCL.Net;
using System.Runtime.InteropServices;

namespace AutoStitch.MatrixProviders
{
    namespace GaussianInternal
    {
        public class OpenCL
        {
            public static OpenCL instance = new OpenCL();
            ~OpenCL() {if(program_initialized) CL.Cl.ReleaseProgram(program); }
            CL.Program program;
            bool program_initialized = false;
            object cl_lock = new object();
            public double[,] WeightedSum(double[,] data, int[] offsets_x, int[] offsets_y, double[] weights)
            {
                System.Diagnostics.Trace.Assert(offsets_x.Length == offsets_y.Length && offsets_y.Length == weights.Length);
                double[,] ans = new double[data.GetLength(0), data.GetLength(1)];
                CL.ErrorCode error;
                byte[] data_byte_array = new byte[Marshal.SizeOf(typeof(double)) * data.Length];
                byte[] offsets_x_byte_array = new byte[Marshal.SizeOf(typeof(int)) * offsets_x.Length];
                byte[] offsets_y_byte_array = new byte[Marshal.SizeOf(typeof(int)) * offsets_y.Length];
                byte[] weights_byte_array = new byte[Marshal.SizeOf(typeof(double)) * weights.Length];
                byte[] dims_byte_array = new byte[Marshal.SizeOf(typeof(int)) * 3];
                byte[] output_byte_array = new byte[Marshal.SizeOf(typeof(double)) * data.Length];
                int[] dims = new[] { data.GetLength(0), data.GetLength(1), offsets_x.Length };
                MyCL.memcpy(ref data, ref data_byte_array);
                MyCL.memcpy(ref offsets_x, ref offsets_x_byte_array);
                MyCL.memcpy(ref offsets_y, ref offsets_y_byte_array);
                MyCL.memcpy(ref weights, ref weights_byte_array);
                MyCL.memcpy(ref dims, ref dims_byte_array);
                lock (cl_lock)
                {
                    if (!program_initialized)
                    {
                        program_initialized = true;
                        string programPath = Path.Combine(Environment.CurrentDirectory, "../../WeightedSum.cl");
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
                            data_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.ReadOnly, data_byte_array, out CL.ErrorCode err1),
                            offsets_x_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.ReadOnly, offsets_x_byte_array, out CL.ErrorCode err2),
                            offsets_y_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.ReadOnly, offsets_y_byte_array, out CL.ErrorCode err3),
                            weights_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.ReadOnly, weights_byte_array, out CL.ErrorCode err4),
                            dims_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.ReadOnly, dims_byte_array, out CL.ErrorCode err5),
                            output_buffer = CL.Cl.CreateBuffer(MyCL.context, CL.MemFlags.CopyHostPtr | CL.MemFlags.WriteOnly, output_byte_array, out CL.ErrorCode err6))
                        {
                            MyCL.CheckErr(err1, "Cl.CreateBuffer data");
                            MyCL.CheckErr(err2, "Cl.CreateBuffer offsets_x");
                            MyCL.CheckErr(err3, "Cl.CreateBuffer offsets_y");
                            MyCL.CheckErr(err4, "Cl.CreateBuffer weights");
                            MyCL.CheckErr(err5, "Cl.CreateBuffer dims");
                            MyCL.CheckErr(err6, "Cl.CreateBuffer output");
                            int intPtrSize = Marshal.SizeOf(typeof(IntPtr));
                            error =
                                CL.Cl.SetKernelArg(kernel, 0, (IntPtr)intPtrSize, data_buffer) |
                                CL.Cl.SetKernelArg(kernel, 1, (IntPtr)intPtrSize, offsets_x_buffer) |
                                CL.Cl.SetKernelArg(kernel, 2, (IntPtr)intPtrSize, offsets_y_buffer) |
                                CL.Cl.SetKernelArg(kernel, 3, (IntPtr)intPtrSize, weights_buffer) |
                                CL.Cl.SetKernelArg(kernel, 4, (IntPtr)intPtrSize, dims_buffer) |
                                CL.Cl.SetKernelArg(kernel, 5, (IntPtr)intPtrSize, output_buffer);
                            MyCL.CheckErr(error, "Cl.SetKernelArg");

                            //Create a command queue, where all of the commands for execution will be added
                            using (CL.CommandQueue cmdQueue = CL.Cl.CreateCommandQueue(MyCL.context, MyCL.device, (CL.CommandQueueProperties)0, out error))
                            {
                                MyCL.CheckErr(error, "Cl.CreateCommandQueue");
                                CL.Event clevent;
                                IntPtr[] workGroupSizePtr = new IntPtr[] { (IntPtr)data.GetLength(0), (IntPtr)data.GetLength(1) };
                                error = CL.Cl.EnqueueNDRangeKernel(
                                    cmdQueue,
                                    kernel,
                                    2,
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
        abstract class GaussianBlurBase : MatrixProvider
        {
            protected IMatrixProvider provider;
            protected double ro;
            protected GaussianBlurBase(IMatrixProvider provider, double ro)
            {
                this.provider = provider;
                this.ro = ro;
            }
            public override void Reset()
            {
                base.Reset();
                provider.Reset();
            }
            protected double Gaussion(double x, double y)
            {
                return Math.Exp(-(x * x + y * y) / (ro * ro)) / (2 * Math.PI * ro * ro);
            }
        }
    }
    sealed class GaussianBlur : GaussianInternal.GaussianBlurBase
    {
        public GaussianBlur(IMatrixProvider provider, double ro,bool speedup=true) : base(speedup? new GaussianBlurY(new GaussianBlurX(provider, ro), ro):provider, ro) {  }
        protected override MyMatrix GetMatrixInternal()
        {
            return provider.GetMatrix();
        }
    }
    sealed class GaussianBlurX : GaussianInternal.GaussianBlurBase
    {
        public GaussianBlurX(IMatrixProvider provider, double ro) : base(provider, ro) { }
        protected override MyMatrix GetMatrixInternal()
        {
            var image = provider.GetMatrix();
            int width = image.data.GetLength(1), height = image.data.GetLength(0);
            double[,] sum = new double[height, width];
            double eps = 1e-9;
            List<int> offsets_x = new List<int>(), offsets_y = new List<int>();
            List<double> weights = new List<double>();
            for (int dx = 0; dx < width && Gaussion(dx, 0) >= eps; dx++)
            {
                offsets_x.Add(dx); offsets_y.Add(0); weights.Add(Gaussion(dx, 0));
                if (dx != 0)
                {
                    dx = -dx;
                    offsets_x.Add(dx); offsets_y.Add(0); weights.Add(Gaussion(dx, 0));
                    dx = -dx;
                }
            }
            return new MyMatrix(GaussianInternal.OpenCL.instance.WeightedSum(image.data, offsets_x.ToArray(), offsets_y.ToArray(), weights.ToArray()));
        }
    }
    sealed class GaussianBlurY : GaussianInternal.GaussianBlurBase
    {
        public GaussianBlurY(IMatrixProvider provider, double ro) : base(provider, ro) { }
        protected override MyMatrix GetMatrixInternal()
        {
            var image = provider.GetMatrix();
            int width = image.data.GetLength(1), height = image.data.GetLength(0);
            double[,] sum = new double[height, width];
            double eps = 1e-9;
            List<int> offsets_x = new List<int>(), offsets_y = new List<int>();
            List<double> weights = new List<double>();
            for (int dy = 0; dy < height && Gaussion(0, dy) >= eps; dy++)
            {
                offsets_x.Add(0); offsets_y.Add(dy); weights.Add(Gaussion(0, dy));
                if (dy != 0)
                {
                    dy = -dy;
                    offsets_x.Add(0); offsets_y.Add(dy); weights.Add(Gaussion(0, dy));
                    dy = -dy;
                }
            }
            return new MyMatrix(GaussianInternal.OpenCL.instance.WeightedSum(image.data, offsets_x.ToArray(), offsets_y.ToArray(), weights.ToArray()));
        }
    }
}
