using System;

namespace GestãoIdeas.Models;

public class Idea
{
     public int Id{get; set;}
    public string? Name{get; set;}
    public string? Description{get; set;}
    public DateTime CreatedAt{get; set;}
    public IdeaState State{get; set;}
    public int Priority{get; set;}
}
