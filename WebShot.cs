using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace WebShooters
{
    internal class WebShot : MonoBehaviour
    {
        Item item;
        WebShooterMono shooterMono;

        WebShooterMode currentMode;

        public void OnStart(WebShooterMono webShooter, bool inheritMode = true)
        {
            item = GetComponent<Item>();

            shooterMono = webShooter;

            if(inheritMode) currentMode = webShooter.currentMode;

            item.mainCollisionHandler.OnCollisionStartEvent += MainCollisionHandler_OnCollisionStartEvent;
        }
        void CreatureHit(RagdollPart part, GameObject plane)
        {
            if (part.ragdoll == null || part.ragdoll.creature == null || part.ragdoll.creature.isPlayer || part.ragdoll.creature.isKilled) return;
            var c = part.ragdoll.creature;
            if (c.ragdoll.state == Ragdoll.State.NoPhysic) c.ragdoll.AddPhysicToggleModifier(this);
            c.TryPush(Creature.PushType.Hit, (transform.forward) * 40, 2, part.type);
            plane.transform.parent = part.transform;
        }

        private void MainCollisionHandler_OnCollisionStartEvent(CollisionInstance collisionInstance)
        {
            shooterMono.Activated = false;
            var plane = Instantiate(shooterMono.webGameObject);
            Creature c = null;
            RagdollPart part = null;
            if (collisionInstance.targetCollider.gameObject.GetComponentInParent<RagdollPart>())
            {
                part = collisionInstance.targetCollider.gameObject.GetComponentInParent<RagdollPart>();
                CreatureHit(part, plane);

                c = part.ragdoll.creature;
            }
            plane.transform.position = collisionInstance.contactPoint;
            plane.transform.rotation = Quaternion.LookRotation(collisionInstance.contactNormal);
            if (currentMode != null)
            {
                if (c != null) currentMode.OnCreatureHit(c, collisionInstance, plane, shooterMono.nozzle, shooterMono, part);
                else currentMode.OnHit(collisionInstance, plane, shooterMono.nozzle, shooterMono);
            }
            Extensions.CalculateRandomScale(plane, c == null ? false : true, 0.5f, 2.0f);
            item.Despawn();
        }
    }
}
