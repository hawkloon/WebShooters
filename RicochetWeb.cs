using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace WebShooters
{
    internal class RicochetWeb : WebShooterMode
    {
        public override string Name => "Ricochet Web";

        public override string Description => "A web that bounces once";

        public override string IconID => "WebShooter.RicochetWebIcon";

        public override bool ShootWeb => true;


        public override void OnHit(CollisionInstance collisionInstance, GameObject web, Transform nozzle, WebShooterMono shooterMono)
        {
            base.OnHit(collisionInstance, web, nozzle, shooterMono);
            var inDir = (collisionInstance.contactPoint - nozzle.position).normalized;

            var outDir = Vector3.Reflect(inDir, collisionInstance.contactNormal);
            Catalog.GetData<ItemData>("WebShot").SpawnAsync(shot =>
            {
                if(WebShooterMaster.WebGravity < 1) shot.mainCollisionHandler.SetPhysicModifier(this, WebShooterMaster.WebGravity);
                shot.physicBody.AddForce(outDir * WebShooterMaster.WebLineForce, ForceMode.Impulse);

                shot.Throw();
                shot.ignoredItem = shooterMono.item;
                shot.ignoredRagdoll = Player.local.creature.ragdoll;
                shot.gameObject.AddComponent<WebShot>().OnStart(shooterMono, false);
            }, collisionInstance.contactPoint + (outDir / 3), Quaternion.LookRotation(outDir));
        }
    }
}
