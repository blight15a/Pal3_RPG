// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [SceCommand(112, "进入山洞初体验游戏")]
    public class MiniGameStartCaveExperienceCommand : ICommand
    {
        public MiniGameStartCaveExperienceCommand() {}
    }
    #endif
}