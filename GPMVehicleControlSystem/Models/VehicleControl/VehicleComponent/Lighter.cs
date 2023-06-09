using GPMVehicleControlSystem.VehicleControl.DIOModule;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace AGVSystemCommonNet6.Abstracts
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
                    await Task.Delay(flash_period, flash_cts.Token);
                }
            });
        }

        public void Flash(DO_ITEM[] light_DOs, int flash_period = 400)
        {
            foreach (var item in light_DOs)
                this.DOModule.SetState(item, true);

            flash_cts = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (flash_cts.IsCancellationRequested)
                        break;

                    bool previous_state_on = DOModule.GetState(light_DOs.First());
                    foreach (var item in light_DOs)
                        this.DOModule.SetState(item, !previous_state_on);
                    await Task.Delay(flash_period, flash_cts.Token);
                }
            });
        }


        public abstract void CloseAll();
        public abstract void OpenAll();
    }
}
