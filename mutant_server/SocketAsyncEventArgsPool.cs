using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace mutant_server
{
    class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> m_pool;
        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }
        public void Push(SocketAsyncEventArgs ev)
        {
            if(ev == null) 
            {
                throw new ArgumentNullException("null SocketAsyncEventArgs tried to push to pool\n"); 
            }
            lock(m_pool)
            {
                m_pool.Push(ev);
            }
        }
        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }
    }
}
