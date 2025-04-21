using System;
using System.Collections.Generic;

namespace DockerX.Core.Model;

public class Container
{
    public required string Id { get; set; }          // CONTAINER ID
    public required string Image { get; set; }       // IMAGE
    public required string Command { get; set; }     // COMMAND
    public DateTime Created { get; set; }   // CREATED
    public required string Status { get; set; }      // STATUS
    public required List<string> Ports { get; set; }       // PORTS
}