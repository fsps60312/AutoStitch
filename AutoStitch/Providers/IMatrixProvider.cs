using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    public delegate void MatrixChangedEventHandler(MyMatrix matrix);
    interface IMatrixProvider
    {
        event MatrixChangedEventHandler MatrixChanged;
        MyMatrix GetMatrix();
        void Reset();
        void ResetSelf();
    }
    public abstract class MatrixProvider : IMatrixProvider
    {
        public event MatrixChangedEventHandler MatrixChanged;
        private MyMatrix ans = null;
        public virtual void Reset() { ResetSelf(); }
        public void ResetSelf() { ans = null; }
        public MyMatrix GetMatrix()
        {
            if(ans==null)
            {
                ans = GetMatrixInternal();
                MatrixChanged?.Invoke(ans);
            }
            return ans;
        }
        protected abstract MyMatrix GetMatrixInternal();
    }
}
