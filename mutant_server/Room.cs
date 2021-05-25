using System.Collections.Generic;

namespace mutant_server
{
    class Room
    {
        Dictionary<int, Client> _players;
        public Room()
        {
            _players = new Dictionary<int, Client>(5);
        }
        public void Update(object elapsedTime)
        {
            var iter = _players.GetEnumerator();
            do
            {
                var token = iter.Current.Value.asyncUserToken;
            } while (iter.MoveNext());
        }
    }
}
