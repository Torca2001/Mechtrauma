/***
Water drain component
***/
using System;
using Barotrauma;
using Barotrauma.Networking;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Barotrauma.Items.Components;
using System.Linq;

namespace Barotrauma.Items.Components 
{
    partial class WaterDrain : Powered {

        [Editable, Serialize(80.0f, IsPropertySaveable.No, description: "How fast the item pumps water in/out when operating at 100%.", alwaysUseInstanceValues: true)]
        public float MaxFlow
        {
            get => maxFlow;
            set => maxFlow = value;
        }
        private float maxFlow = 80.0f;

        [Editable, Serialize(true, IsPropertySaveable.Yes, alwaysUseInstanceValues: true)]

        public bool IsInfected 
        {
            get => isInfected;
            set => isInfected = value;
        }
        private bool isInfected = false;

        public float CurrFlow
        {
            get => Math.Abs(currFlow);
        }
        private float currFlow;

        public float FlowPercentage
        {
            get => currFlow / MaxFlow * 100.0f;
        }

        public override bool UpdateWhenInactive => true;

        private float isActiveLockTimer;

        public WaterDrain(Item item, ContentXElement element) : base(item, element) 
        {
            IsActive = true;
            InitProjSpecific(element);
        }

        partial void InitProjSpecific(ContentXElement element);

        partial void UpdateProjSpecific(float deltaTime);

        public override void Update(float deltaTime, Camera cam)
        {
            isActiveLockTimer -= deltaTime;

            if (!IsActive)
            {
                return;
            }

            UpdateProjSpecific(deltaTime);

            ApplyStatusEffects(ActionType.OnActive, deltaTime, null);

            if (item.CurrentHull == null) { return; }

            currFlow = MaxFlow * Voltage;
            item.CurrentHull.WaterVolume += currFlow * deltaTime * Timing.FixedUpdateRate; 
            if (item.CurrentHull.WaterVolume > item.CurrentHull.Volume) { item.CurrentHull.Pressure += 30.0f * deltaTime; }
        }

        /// <summary>
        /// Power consumption of the Pump. Only consume power when active and adjust consumption based on condition.
        /// </summary>
        public override float GetCurrentPowerConsumption(Connection connection = null)
        {
            //There shouldn't be other power connections to this
            if (connection == powerOut && IsActive)
            {
                return MaxFlow;
            }

            return 0;
        }

        public override void GridResolved(Connection conn)
        {
            if (conn == powerOut)
            {
                // Correct voltage to allow negative voltage
                if (powerOut.Grid != null && powerOut.Grid.Power < 0 && powerOut.Grid.Voltage == 0) {
                    float newVoltage = powerOut.Grid.Power / MathHelper.Max(powerOut.Grid.Load, 1E-10f);
                    
                    // Clamp voltage between -1000 to 1000
                    if (newVoltage > 1000) {
                        newVoltage = 1000.0f;
                    } else if (newVoltage < -1000) {
                        newVoltage = -1000;
                    }
                    
                    powerOut.Grid.Voltage = newVoltage;
                }
            }
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (IsInfected) { return; }

            if (connection.Name == "toggle")
            {
                IsActive = !IsActive;
                isActiveLockTimer = 0.1f;
            }
            else if (connection.Name == "set_active")
            {
                IsActive = signal.value != "0";
                isActiveLockTimer = 0.1f;
            }
        }

    }
}