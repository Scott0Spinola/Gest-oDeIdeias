using System.Collections.Generic;

namespace GestãoIdeas.DTOs;

public record IdeasWithAdviceResponse(
    List<IdeaDTO> Ideas,
    string Advice
);