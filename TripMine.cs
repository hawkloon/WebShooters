using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace WebShooters
{
    internal class TripMine : WebShooterMode
    {

        public override string Name => "Trip Mine";

        public override string Description => "When an enemy walks infront of this, it pulls them towards it!";

        public override string IconID => "WebShooter.TripMineIcon";

        public override bool ShootWeb => true;

        public override void OnHit(CollisionInstance collisionInstance, GameObject web, Transform nozzle, WebShooterMono shooterMono)
        {
            base.OnHit(collisionInstance, web, nozzle, shooterMono);
            Catalog.InstantiateAsync("WebShooter.TripMine", collisionInstance.contactPoint, Quaternion.LookRotation(collisionInstance.contactNormal), null, mine =>
            {
                mine.AddComponent<TripMineMono>().OnStart(shooterMono);
            }, "Trip Mine");
        }
    }

    public class TripMineMono : MonoBehaviour
    {
        bool used;

        WebShooterMono webShooter;

        public void OnStart(WebShooterMono shooterMono)
        {
            used = false;
            webShooter = shooterMono;

        }
        private void Start()
        {
            used = false;
        }

        public void DestabilizeFromPhysicBody(PhysicBody body)
        {
            if (body == null) return;
            if (body.gameObject.GetComponent<RagdollPart>() is RagdollPart part)
            {
                if (part.ragdoll && part.ragdoll.creature && !part.ragdoll.creature.isKilled)
                {
                    part.ragdoll.SetState(Ragdoll.State.Destabilized);
                    part.ragdoll.creature.brain.AddNoStandUpModifier(body);
                }
            }

            else if (body.gameObject.GetComponentInParent<RagdollPart>() is RagdollPart part2)
            {
                if (part2.ragdoll && part2.ragdoll.creature && !part2.ragdoll.creature.isKilled)
                {
                    part2.ragdoll.SetState(Ragdoll.State.Destabilized);
                    part2.ragdoll.creature.brain.AddNoStandUpModifier(body);
                }
            }
        }

        public void CreateJoint(Creature creature)
        {
            Debug.Log($"Trip Mine Creating Joint");
            var game = new GameObject();
            game.transform.rotation = Quaternion.identity;
            var rb = game.AddComponent<Rigidbody>();
            creature.brain.AddNoStandUpModifier(game);

            var line = game.AddComponent<LineRenderer>();
            line.material = webShooter.lineMaterial;
            line.startWidth = 0.05f;
            line.textureMode = LineTextureMode.Tile;
            line.endWidth = 0.05f;

            game.transform.position = transform.position;

            var joint = creature.ragdoll.rootPart.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = rb;
            joint.connectedAnchor = Vector3.zero;
            joint.spring = 2500f;
            joint.damper = 150f;
            joint.maxDistance = 0.1f;

            game.AddComponent<FixedJoint>();

            var e = game.AddComponent<TetherUpdate>();
            e.OnStart(creature);
            e.start = transform;
            e.end = creature.ragdoll.rootPart.transform;
            used = true;
            creature.ragdoll.rootPart.physicBody.AddForce((transform.position - creature.ragdoll.rootPart.transform.position).normalized * 60, ForceMode.Impulse);
            Destroy(this);
        }

        public void TryRaycastCreature()
        {
            if(Physics.SphereCast(transform.position, 0.15f, transform.forward, out RaycastHit hit, 5f))
            {
                if (Extensions.GetCreatureFromRaycast(hit) is Creature c)
                {
                    if (c.isPlayer || c.isKilled) return;

                    c.ragdoll.SetState(Ragdoll.State.Destabilized);
                    CreateJoint(c);
                }
            }
        }

        private void Update()
        {
            if (used) return;

            TryRaycastCreature();
        }
    }
}
