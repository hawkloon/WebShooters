using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ThunderRoad;

namespace WebShooters
{
    public class WebShooterMode
    {
        public virtual string Name { get; set; }

        public virtual string IconID { get; set; }

        public virtual Color IconColor { get; set; }
        public virtual string Description { get; set; }

        public virtual bool RunHitAfterCreatureHit { get; set; }

        public virtual bool ShootWeb { get; set; }

        public virtual void Init()
        {
            //Catalog.LoadAssetAsync<Image>($"{IconID}", callback => { icon = callback; }, $"{IconID}");
        }                       

        public virtual void OnActivate(WebShooterMono shooterMono)
        {

        }

        public virtual void OnHit(CollisionInstance collisionInstance, GameObject web, Transform nozzle, WebShooterMono shooterMono)
        {

        }

        public virtual void OnCreatureHit(Creature creature, CollisionInstance collisionInstance, GameObject web, Transform nozzle, WebShooterMono shooterMono, RagdollPart part = null)
        {
            if (RunHitAfterCreatureHit) OnHit(collisionInstance, web, nozzle, shooterMono);
        }
    }
}
