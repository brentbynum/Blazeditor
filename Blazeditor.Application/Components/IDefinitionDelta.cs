using Blazeditor.Application.Models;
using System.Collections.Generic;

public interface IDefinitionDelta
{
    void Apply(Definition definition);
    void Revert(Definition definition);
}