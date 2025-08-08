using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class TuneController : ControllerBase
{
    private readonly KitharaDbContext _db;
    public TuneController(KitharaDbContext db) => _db = db;

    [HttpGet]
    public ActionResult<IEnumerable<Tune>> Get() => _db.Tunes.ToList();

    [HttpGet("{id}")]
    public ActionResult<Tune> Get(Guid id)
    {
        var tune = _db.Tunes.Find(id);
        return tune is not null ? Ok(tune) : NotFound();
    }

    [HttpPost]
    public ActionResult<Tune> Post(Tune tune)
    {
        tune.Id = Guid.NewGuid();
        _db.Tunes.Add(tune);
        _db.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = tune.Id }, tune);
    }

    [HttpPut("{id}")]
    public IActionResult Put(Guid id, Tune updated)
    {
        var tune = _db.Tunes.Find(id);
        if (tune is null) return NotFound();
        updated.Id = id;
        _db.Entry(tune).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var tune = _db.Tunes.Find(id);
        if (tune is null) return NotFound();
        _db.Tunes.Remove(tune);
        _db.SaveChanges();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class PlaylistController : ControllerBase
{
    private readonly KitharaDbContext _db;
    public PlaylistController(KitharaDbContext db) => _db = db;

    [HttpGet]
    public ActionResult<IEnumerable<Playlist>> Get() => _db.Playlists.ToList();

    [HttpGet("{id}")]
    public ActionResult<Playlist> Get(Guid id)
    {
        var playlist = _db.Playlists.Find(id);
        return playlist is not null ? Ok(playlist) : NotFound();
    }

    [HttpPost]
    public ActionResult<Playlist> Post(Playlist playlist)
    {
        playlist.Id = Guid.NewGuid();
        _db.Playlists.Add(playlist);
        _db.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = playlist.Id }, playlist);
    }

    [HttpPut("{id}")]
    public IActionResult Put(Guid id, Playlist updated)
    {
        var playlist = _db.Playlists.Find(id);
        if (playlist is null) return NotFound();
        updated.Id = id;
        _db.Entry(playlist).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var playlist = _db.Playlists.Find(id);
        if (playlist is null) return NotFound();
        _db.Playlists.Remove(playlist);
        _db.SaveChanges();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class StrunaController : ControllerBase
{
    private readonly KitharaDbContext _db;
    public StrunaController(KitharaDbContext db) => _db = db;

    [HttpGet]
    public ActionResult<IEnumerable<Struna>> Get() => _db.Strunas.ToList();

    [HttpGet("{id}")]
    public ActionResult<Struna> Get(Guid id)
    {
        var struna = _db.Strunas.Find(id);
        return struna is not null ? Ok(struna) : NotFound();
    }

    [HttpPost]
    public ActionResult<Struna> Post(Struna struna)
    {
        struna.Id = Guid.NewGuid();
        _db.Strunas.Add(struna);
        _db.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = struna.Id }, struna);
    }

    [HttpPut("{id}")]
    public IActionResult Put(Guid id, Struna updated)
    {
        var struna = _db.Strunas.Find(id);
        if (struna is null) return NotFound();
        updated.Id = id;
        _db.Entry(struna).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var struna = _db.Strunas.Find(id);
        if (struna is null) return NotFound();
        _db.Strunas.Remove(struna);
        _db.SaveChanges();
        return NoContent();
    }
}
