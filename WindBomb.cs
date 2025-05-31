using UnityEngine;
using MoreSlugcats;
using RWCustom;
using Expedition;
using Noise;

namespace WindBombs;

public class WindBomb : ScavengerBomb
{
    public bool isGravity;
    public bool thrown;
    public WindBomb(AbstractPhysicalObject obj, World world) : base(obj, world)
    {
        isGravity = this.abstractPhysicalObject.type == WindBombMod.GravityBomb;
        explodeColor = new Color32(171, 171, 203, 255);
    }

    public override void HitByExplosion(float hitFac, Explosion explosion, int hitChunk)
    {
        if (Random.value < hitFac)
        {
            if (this.thrownBy == null && explosion != null)
            {
                this.thrownBy = explosion.killTagHolder;
            }
            this.WindExplode(null);
        }
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        color = new Color32(82, 82, 102, 255);
        this.UpdateColor(sLeaser, color);
    }

    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        if (thrown && !(this.floorBounceFrames > 0 && (direction.x == 0 || this.room.GetTile(base.firstChunk.pos).Terrain == Room.Tile.TerrainType.Slope)))
        {
            ignited = true;
        }
        base.TerrainImpact(chunk, direction, speed, firstContact);
    }

    public override void Thrown(Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
    {
        base.Thrown(thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
        ignited = false;
        thrown = true;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (ignited == true || burn > 0f)
        {
            this.WindExplode(null);
        }
    }

    public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
    {
        if (result.obj == null)
        {
            return false;
        }
        if (ModManager.Watcher && result.obj.abstractPhysicalObject.rippleLayer != this.abstractPhysicalObject.rippleLayer && !result.obj.abstractPhysicalObject.rippleBothSides && !this.abstractPhysicalObject.rippleBothSides)
        {
            return false;
        }
        this.vibrate = 20;
        this.ChangeMode(Weapon.Mode.Free);
        if (result.chunk != null)
        {
            result.chunk.vel += base.firstChunk.vel * base.firstChunk.mass / result.chunk.mass;
        }
        else if (result.onAppendagePos != null)
        {
            (result.obj as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, new Vector2(0, 0.5f) * base.firstChunk.mass);
        }
        this.WindExplode(result.chunk);
        return true;
    }

    public override void HitByWeapon(Weapon weapon)
    {
        if (weapon.mode == Weapon.Mode.Thrown && this.thrownBy == null && weapon.thrownBy != null)
        {
            this.thrownBy = weapon.thrownBy;
        }
        base.HitByWeapon(weapon);
        WindExplode(null);
    }

    public void WindExplode(BodyChunk hitChunk)
    {
        if (MeadowCompat.MeadowEnabled && MeadowCompat.ExplodeRPC(this))
        {
            return;
        }
        if (slatedForDeletetion)
        {
            return;
        }
        Vector2 vector = Vector2.Lerp(firstChunk.pos, firstChunk.lastPos, 0.35f);
        //room.AddObject(new SootMark(room, vector, 80f, true));
        if (!explosionIsForShow)
        {
            room.AddObject(new WindExplosion(room, this, vector, 7, 250f, 6.2f, 60f, thrownBy, 0.7f, 22f, 1f, isGravity));
        }
        room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, explodeColor));
        room.AddObject(new ExplosionSpikes(room, vector, 14, 30f, 7f, 7f, 170f, explodeColor));
        room.ScreenMovement(new Vector2?(vector), default(Vector2), 0.45f);
        for (int m = 0; m < abstractPhysicalObject.stuckObjects.Count; m++)
        {
            abstractPhysicalObject.stuckObjects[m].Deactivate();
        }
        room.PlaySound(SoundID.Vulture_Wing_Woosh_LOOP, vector, 1f, 1.4f, abstractPhysicalObject);
        room.InGameNoise(new InGameNoise(vector, 1000f, this, 1f));
        bool flag = hitChunk != null;
        if (smoke != null)
        {
            smoke.Destroy();
        }
        Destroy();
    }

    public class WindExplosion : Explosion
    {
        public bool isGravity;
        public WindExplosion(Room room, PhysicalObject sourceObject, Vector2 pos, int lifeTime, float rad, float force, float stun, Creature killTagHolder, float killTagHolderDmgFactor, float minStun, float backgroundNoise, bool isGravity) : base(room, sourceObject, pos, lifeTime, rad, force, 0f, 30f, 0f, killTagHolder, killTagHolderDmgFactor, minStun, backgroundNoise)
        {
            this.isGravity = isGravity;
        }

        public Vector2 GetAngle(Vector2 A, Vector2 B)
        {
            return isGravity ? -(Vector2)Vector3.Slerp((B - A).normalized, new Vector2(0f, 1f), 0.2f) : (Vector2)Vector3.Slerp((B - A).normalized, new Vector2(0f, 0.5f), 0.8f);
        }

        public override void Update(bool eu)
        {
            evenUpdate = eu;
            if (!this.explosionReactorsNotified)
            {
                this.explosionReactorsNotified = true;
                if (this.sourceObject != null)
                {
                    this.room.InGameNoise(new InGameNoise(this.pos, this.backgroundNoise * 2700f, this.sourceObject, this.backgroundNoise * 6f));
                }
            }
            this.room.MakeBackgroundNoise(this.backgroundNoise);
            float num = this.rad * (0.25f + 0.75f * Mathf.Sin(Mathf.InverseLerp(0f, (float)this.lifeTime, (float)this.frame) * 3.1415927f));
            for (int j = 0; j < this.room.physicalObjects.Length; j++)
            {
                for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
                {
                    if (this.sourceObject != this.room.physicalObjects[j][k] && (this.sourceObject == null || !ModManager.Watcher || this.sourceObject.abstractPhysicalObject.rippleLayer == this.room.physicalObjects[j][k].abstractPhysicalObject.rippleLayer || this.sourceObject.abstractPhysicalObject.rippleBothSides || ModManager.MSC && this.room.physicalObjects[j][k].abstractPhysicalObject.rippleBothSides) && !this.room.physicalObjects[j][k].slatedForDeletetion)
                    {
                        float num2 = 0f;
                        float num3 = float.MaxValue;
                        int num4 = -1;
                        for (int l = 0; l < this.room.physicalObjects[j][k].bodyChunks.Length; l++)
                        {
                            float num5 = Vector2.Distance(this.pos, this.room.physicalObjects[j][k].bodyChunks[l].pos);
                            if (num5 < 100f) num5 = 100f;
                            num3 = Mathf.Min(num3, num5);
                            if (num5 < num)
                            {
                                float num6 = Mathf.InverseLerp(num, num * 0.25f, num5);
                                if (num6 > 77f) num6 = 77f;
                                if (!this.room.VisualContact(this.pos, this.room.physicalObjects[j][k].bodyChunks[l].pos))
                                {
                                    num6 -= 0.5f;
                                }
                                if (num6 > 0f)
                                {
                                    float num7 = this.force;
                                    if (ModManager.MSC && this.room.physicalObjects[j][k] is Player player && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                                    {
                                        num7 *= 0.5f;
                                    }
                                    var angle = GetAngle(this.pos, this.room.physicalObjects[j][k].bodyChunks[l].pos);
                                    this.room.physicalObjects[j][k].bodyChunks[l].vel += angle * (num7 / this.room.physicalObjects[j][k].bodyChunks[l].mass) * num6;
                                    this.room.physicalObjects[j][k].bodyChunks[l].pos += angle * (num7 / this.room.physicalObjects[j][k].bodyChunks[l].mass) * num6 * 0.1f;
                                    if (num6 > num2)
                                    {
                                        num2 = num6;
                                        num4 = l;
                                    }
                                }
                            }
                        }
                        if (this.room.physicalObjects[j][k] == this.killTagHolder)
                        {
                            num2 = 0f;
                        }
                        if (num4 > -1)
                        {
                            if (this.room.physicalObjects[j][k] is Creature)
                            {
                                if (this.killTagHolder != null && this.room.physicalObjects[j][k] != this.killTagHolder)
                                {
                                    (this.room.physicalObjects[j][k] as Creature).SetKillTag(this.killTagHolder.abstractCreature);
                                }
                                if (this.minStun > 0f && (!ModManager.MSC || !(this.room.physicalObjects[j][k] is Player) || (this.room.physicalObjects[j][k] as Player).SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer || (ModManager.Expedition && this.room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("unl-explosionimmunity"))))
                                {
                                    (this.room.physicalObjects[j][k] as Creature).Stun((int)(this.minStun * Mathf.InverseLerp(0f, 0.5f, num2)));
                                }
                                if ((this.room.physicalObjects[j][k] as Creature).graphicsModule != null && (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts != null)
                                {
                                    for (int m = 0; m < (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts.Length; m++)
                                    {
                                        float num10 = this.force;
                                        if ((ModManager.MSC && this.room.physicalObjects[j][k] is Player && (this.room.physicalObjects[j][k] as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer) || (ModManager.Expedition && this.room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("unl-explosionimmunity")))
                                        {
                                            num10 *= 0.25f;
                                        }
                                        var angle = GetAngle(this.pos, (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos);
                                        (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos += angle * num2 * num10 * 5f;
                                        (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].vel += angle * num2 * num10 * 5f;
                                        if ((this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m] is Limb)
                                        {
                                            ((this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m] as Limb).mode = Limb.Mode.Dangle;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            this.frame++;
            if (this.frame > this.lifeTime)
            {
                this.Destroy();
            }
        }
    }
}