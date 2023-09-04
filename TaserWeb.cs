using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace WebShooters
{
    internal class TaserWeb : WebShooterMode
    {
        public override string Name => "Taser Web";
        public override string Description => "Shock your enemies with 5000 volts via webbing!";

        public override string IconID => "WebShooter.TaserWebIcon";

        public override bool RunHitAfterCreatureHit => true;
        public override bool ShootWeb => true;

        public override void OnHit(CollisionInstance collisionInstance, GameObject web, Transform nozzle, WebShooterMono shooterMono)
        {
            base.OnHit(collisionInstance, web, nozzle, shooterMono);
            var lightning = Catalog.GetData<EffectData>("ImbueLightningVfx").Spawn(web.transform);
            lightning.SetTarget(web.transform);
            lightning.SetRenderer(web.GetComponentInChildren<Renderer>(), false);
            lightning.SetIntensity(100f);
            lightning.Play();
        }

        public override void OnCreatureHit(Creature creature, CollisionInstance collisionInstance, GameObject web, Transform nozzle, WebShooterMono shooterMono, RagdollPart part = null)
        {
            base.OnCreatureHit(creature, collisionInstance, web, nozzle, shooterMono, part);
            creature.TryElectrocute(15f, 2f, true, true, Catalog.GetData<EffectData>("ImbueLightningRagdoll"));
        }
    }
}
