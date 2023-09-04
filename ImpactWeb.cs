using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
namespace WebShooters
{
    internal class ImpactWeb : WebShooterMode
    {
        public override string Name => "Impact Web";
        public override string Description => "This one packs a punch";

        public override string IconID => "WebShooter.ImpactWebIcon";

        public override bool RunHitAfterCreatureHit => false;

        public override bool ShootWeb => true;

        private RagdollPart affectedPart;

        private GameObject webGame;

        private CollisionInstance collIns;
        public override void OnCreatureHit(Creature creature, CollisionInstance collisionInstance, GameObject web,Transform nozzle, WebShooterMono shooterMono, RagdollPart part = null)
        {
            base.OnCreatureHit(creature, collisionInstance, web, nozzle, shooterMono, part);
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
            part.physicBody.AddForce((collisionInstance.contactPoint - nozzle.position).normalized * 500f, ForceMode.Impulse);
            affectedPart = part;
            webGame = shooterMono.webGameObject;
            collIns = collisionInstance;
            creature.ragdoll.OnContactStartEvent += Ragdoll_OnContactStartEvent;
            GameManager.local.StartCoroutine(ImpactCountdown(creature));
        }

        private void Ragdoll_OnContactStartEvent(CollisionInstance collisionInstance, RagdollPart ragdollPart)
        {
            Debug.Log($"Ragdoll hit something!");
            Debug.Log(collisionInstance.targetCollider.name + " Target Collider" + collisionInstance.targetCollider.gameObject.layer);
            Debug.Log(collisionInstance.sourceCollider.name + " Source Collider") ;
            if (collisionInstance.targetCollider.GetComponentInParent<Item>() || collisionInstance.targetCollider.GetComponentInParent<Creature>() || collisionInstance.intensity < 0.5f || collisionInstance.targetCollider.gameObject.layer != GameManager.GetLayer(LayerName.Default)) return;
            Debug.Log($"It didn't hit an item, creature and was a strong enough impact");
            if (affectedPart == null) return;
            Debug.Log($"affected part wasn't null");
            var joint = affectedPart.physicBody.rigidBody.gameObject.AddComponent<FixedJoint>();
            joint.connectedAnchor = collisionInstance.contactPoint;
            var jointWeb = GameObject.Instantiate(webGame, collisionInstance.targetCollider.transform);
            jointWeb.transform.position = affectedPart.transform.position + (collIns.contactPoint - affectedPart.transform.position).normalized / 3;
            jointWeb.transform.rotation = Quaternion.LookRotation(collIns.contactNormal);
            jointWeb.transform.localScale = new Vector3(6, 11.6803846f, 7.07299995f);
            affectedPart.ragdoll.OnContactStartEvent -= Ragdoll_OnContactStartEvent;
            affectedPart = null;
        }

        private IEnumerator ImpactCountdown(Creature creature)
        {
            yield return Yielders.ForSeconds(1.5f);
            affectedPart = null;
            creature.ragdoll.OnContactStartEvent -= Ragdoll_OnContactStartEvent;
        }
    }
}
