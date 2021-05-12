using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace mutant_server
{
    class Timer
    {
        Thread thread;
        private float _baseTime = 0;
        public UpdateMethod updateMethod;
        public Timer()
        {
            thread = new Thread(Update);
            thread.Start();
        }
        private void Update()
        {
            while(true)
            {
                _baseTime += (float)(DateTime.Now.Millisecond * 0.001);
                if(_baseTime > Defines.FrameRate)
                {
                    _baseTime -= Defines.FrameRate;
                    updateMethod.Invoke(Defines.FrameRate);
                }
            }
        }
    }
}
