using DG.Tweening.Core.Easing;
using UnityEngine;
using Zenject;
using Zenject.SpaceFighter;

public class GameInstaller : MonoInstaller
{
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStateSwitcher playerStateSwitcher;
    [SerializeField] private SmileManController smileManController;
    [SerializeField] private ScreenClueManager screenClueManager;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private PlotActionManager plotActionManager;
    [SerializeField] private InputSettings inputSettings;

    public override void InstallBindings()
    {
        Container.Bind<PlayerController>()
            .FromInstance(player)
            .AsSingle();
        Container.Bind<PlayerInventory>()
            .FromInstance(playerInventory)
            .AsSingle();
        Container.Bind<SmileManController>()
            .FromInstance(smileManController)
            .AsSingle();
        Container.Bind<ScreenClueManager>()
            .FromInstance(screenClueManager)
            .AsSingle();
        Container.Bind<CutsceneManager>()
            .FromInstance(cutsceneManager)
            .AsSingle();
        Container.Bind<PlotActionManager>()
            .FromInstance(plotActionManager)
            .AsSingle();
        Container.Bind<InputSettings>()
            .FromInstance(inputSettings)
            .AsSingle();
        Container.Bind<PlayerStateSwitcher>()
            .FromInstance(playerStateSwitcher)
            .AsSingle();
    }
}
