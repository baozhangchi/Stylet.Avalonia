﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylet.Samples.HelloDialog;
internal class AppBootstrapper : Bootstrapper<ShellViewModel>
{
    protected override void ConfigureIoC(StyletIoC.IStyletIoCBuilder builder)
    {
        base.ConfigureIoC(builder);

        builder.Bind<IDialogFactory>().ToAbstractFactory();
    }
}