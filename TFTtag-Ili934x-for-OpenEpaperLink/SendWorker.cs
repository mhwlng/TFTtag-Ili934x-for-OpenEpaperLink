using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTtag_Ili934x_for_OpenEpaperLink
{
    public class SendWorker : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Udp.NetProcessDataReq(0xFC);

            while (!stoppingToken.IsCancellationRequested)
            {
                Udp.NetProcessDataReq(0);

                await Task.Delay(60000, stoppingToken);
            }
        }
    }
}
