using GPMVehicleControlSystem.Models.VehicleControl;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;
using static GPMVehicleControlSystem.Models.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.Abstracts
{
    public abstract class Lighter
    {
        public Lighter(clsDOModule DOModule)
        {
            this.DOModule = DOModule;
        }
        CancellationTokenSource flash_cts = new CancellationTokenSource();

        public clsDOModule DOModule { get; }
        public void AbortFlash()
        {
            flash_cts.Cancel();
        }
        public void Flash(DO_ITEM light_DO, int flash_period = 400)
        {
            this.DOModule.SetState(light_DO, true);

            flash_cts = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (flash_cts.IsCancellationRequested)
                        break;

                    bool previous_state_on = DOModule.GetState(light_DO);
                    this.DOModule.SetState(light_DO, !previous_state_on);
                    await Task.Delay(flash_period);
                }
            });
        }
        public abstract void CloseAll();
        public abstract void OpenAll();
    }
}
