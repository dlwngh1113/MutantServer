using System.Collections.Generic;

namespace mutant_server
{
    class Room
    {
        Dictionary<int, Client> _players;
        public Room()
        {
            _players = new Dictionary<int, Client>(5);
            Server.timer.updateMethod += this.Update;
        }
        public void Update(float elapsedTime)
        {
            var iter = _players.GetEnumerator();
            do
            {
                var token = iter.Current.Value.asyncUserToken;
            } while (iter.MoveNext());
        }
    }
}
