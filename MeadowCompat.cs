using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindBombs;

public static class MeadowCompat
{
    public static bool MeadowEnabled;
    // copied code from rain meadow's explode hooks
    public static bool ExplodeRPC(WindBomb self)
    {
        if (OnlineManager.lobby != null)
        {
            if (!self.abstractPhysicalObject.GetOnlineObject(out var opo))
            {
                RainMeadow.RainMeadow.Error($"Entity {self} doesn't exist in online space!");
                return false;
            }
            if (opo.roomSession.isOwner && (opo.isMine || RPCEvent.currentRPCEvent is not null))
            {
                opo.BroadcastRPCInRoom(opo.Explode, self.bodyChunks[0].pos);
            }
            else if (RPCEvent.currentRPCEvent is null)
            {
                if (!opo.isMine) return true;  // wait to be RPC'd
                opo.roomSession.owner.InvokeOnceRPC(opo.Explode, self.bodyChunks[0].pos);
            }
        }
        return false;
    }
}
