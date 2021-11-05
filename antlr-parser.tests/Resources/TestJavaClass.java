package com.example;

public class Actor {

     class InternalActor {
        public Boolean Verbose;

        public InternalActor(Boolean verbose) {
            Verbose = verbose;
        }

        public void Parse(){
            System.out.println("Parse executed");
        }

         public void DoExtraWork(){
             System.out.println("Extra work");
             DoInternalWork();
         }

         private void DoInternalWork(){
             System.out.println("Internal work");
         }
    }

    private String name;
    private int index;
    private String intelligence = "Low";

    public Actor(String name, int index) {
        this.name = name;
        this.index = index;
    }

    public void doWork(){
        System.out.println(name + " with " + intelligence + " does work");

        InternalActor internalActor = new InternalActor(true);
        internalActor.Parse();

        PushInteranlActor(internalActor);

        doWork1();
    }

    public void doExtraWork(){
        InternalActor internalActor = new InternalActor(true);
        internalActor.Parse();
        RunActor(internalActor);
    }

    private void RunActor(InternalActor internalActor)
    {
        internalActor.DoExtraWork();
    }

    private InternalActor PushInteranlActor(InternalActor intActor){
        return intActor;
    }

    public void changeIntelligence(String newName){
        name = newName;
    }

    public String getName(){
        return name + "" + index;
    }

    public void doWork1(){
        doWork2();
    }

    public void doWork2(){
        System.out.println(name + " with " + intelligence + " does work2");
    }
}
