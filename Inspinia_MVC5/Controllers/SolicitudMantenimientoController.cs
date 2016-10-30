﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Modelo;
using Inspinia_MVC5.Tags;

namespace Inspinia_MVC5.Controllers
{
    [PermisoAttribute()]
    public class SolicitudMantenimientoController : Controller
    {
        private Contexto db = new Contexto();

        [PermisoAttribute(Accion = "Index", Controlador = "SolicitudMantenimiento")]
        public async Task<ActionResult> Index()
        {
            var solicitudmantenimiento = db.SolicitudMantenimiento.Where(x=>x.Activo == true && x.Desaprobado == false).Include(s => s.Usuario).Include(s => s.Vehiculo);

            foreach (var s in solicitudmantenimiento)
            {
                if (s.TipoMantenimiento == null)
                {
                    continue;
                }
                if (s.TipoMantenimiento.Trim().Equals("MANPRE"))
                {
                    s.TipoMantenimiento = "Mantenimiento Preventivo";
                }
                else if (s.TipoMantenimiento.Trim().Equals("MANCOR"))
                {
                    s.TipoMantenimiento = "Mantenimiento Correctivo";
                }
            }
            
            return View(await solicitudmantenimiento.ToListAsync());
        }

        [PermisoAttribute(Accion = "Create", Controlador = "SolicitudMantenimiento")]
        public ActionResult Create(SolicitudMantenimiento solicitudMantenimiento = null, bool xyz = false)
        {

            Usuario usu = (Usuario)Session["Usuario"];

            ViewBag.IdChofer = new SelectList(db.Usuario.Where(x=>x.IdUsuario == usu.IdUsuario), "IdUsuario", "Nombres");
            ViewBag.IdVehiculo = new SelectList(db.VehiculoChofer.Where(x=>x.IdChofer == usu.IdUsuario && x.Activo == true)
                                                                .Select(x => new { idVehiculo = x.Vehiculo.idVehiculo, placa = x.Vehiculo.placaLetras + x.Vehiculo.placaNumeros }), "idVehiculo", "placa");
            if (solicitudMantenimiento.TipoMantenimiento == null)
            {
                solicitudMantenimiento.TipoMantenimiento = "MANPRE";
            }

            ViewBag.TipoMantenimiento = new SelectList(db.Parametros.Where(x => x.codigo == "MANPRE" || x.codigo == "MANCOR" && x.activo == true)
                                                                    .Select(x => new { codigo = x.codigo, valor = x.valor_cadena_1 }),"codigo","valor");
            string[] subMan = db.Parametros.Where(x => x.codigo == solicitudMantenimiento.TipoMantenimiento && x.activo == true).Select(x => x.valor_cadena_2).FirstOrDefault().Split(';');
            ViewBag.SubTipoMantimiento = new SelectList(subMan.Select((r, index) => new { Text = r, Value = index }), "Text", "Text");

            if (solicitudMantenimiento.FechaIngreso < DateTime.Today.AddYears(-1000))
            {
                solicitudMantenimiento.FechaIngreso = DateTime.Today;
                solicitudMantenimiento.FechaEstimadaSalida = DateTime.Today.AddDays(1);
            }

           return View(solicitudMantenimiento);
            
        }

        [HttpPost]
        public ActionResult ComboMantenimiento(SolicitudMantenimiento solicitudMantenimiento)
        {
            return RedirectToAction("Create", solicitudMantenimiento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(SolicitudMantenimiento solicitudMantenimiento)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    solicitudMantenimiento.Activo = true;
                    db.SolicitudMantenimiento.Add(solicitudMantenimiento);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Verifique la información relacionada con los datos, no se ha logrado completar la acción.");
                }
                
            }

            Usuario usu = (Usuario)Session["Usuario"];
            ViewBag.IdChofer = new SelectList(db.Usuario.Where(x => x.IdUsuario == usu.IdUsuario), "IdUsuario", "Nombres");
            ViewBag.IdVehiculo = new SelectList(db.VehiculoChofer.Where(x => x.IdChofer == usu.IdUsuario && x.Activo == true).Select(x => new { idVehiculo = x.Vehiculo.idVehiculo, placa = x.Vehiculo.placaLetras + x.Vehiculo.placaNumeros }), "idVehiculo", "placa");

            ViewBag.TipoMantenimiento = new SelectList(db.Parametros.Where(x => x.codigo == "MANPRE" || x.codigo == "MANCOR" && x.activo == true)
                                                                    .Select(x => new { codigo = x.codigo, valor = x.valor_cadena_1 }), "codigo", "valor", solicitudMantenimiento.TipoMantenimiento);
            string[] subMan = db.Parametros.Where(x => x.codigo == solicitudMantenimiento.TipoMantenimiento && x.activo == true).Select(x => x.valor_cadena_2).FirstOrDefault().Split(';');
            ViewBag.SubTipoMantimiento = new SelectList(subMan.Select((r, index) => new { Text = r, Value = index }), "Text", "Text", solicitudMantenimiento.SubTipoMantimiento);

            return View(solicitudMantenimiento);
        }

        [PermisoAttribute(Accion = "Edit", Controlador = "SolicitudMantenimiento")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SolicitudMantenimiento solicitudMantenimiento = await db.SolicitudMantenimiento.FindAsync(id);
            if (solicitudMantenimiento == null)
            {
                return HttpNotFound();
            }
            Usuario usu = (Usuario)Session["Usuario"];
            ViewBag.IdChofer = new SelectList(db.Usuario.Where(x => x.IdUsuario == usu.IdUsuario), "IdUsuario", "Nombres");
            ViewBag.IdVehiculo = new SelectList(db.VehiculoChofer.Where(x => x.IdChofer == usu.IdUsuario && x.Activo == true).Select(x => new { idVehiculo = x.Vehiculo.idVehiculo, placa = x.Vehiculo.placaLetras + x.Vehiculo.placaNumeros }), "idVehiculo", "placa");
            return View(solicitudMantenimiento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include="IdSolicitud,IdVehiculo,IdChofer,TipoMantenimiento,Detalle,FechaIngreso,FechaEstimadaSalida,Aprobado,Activo")] SolicitudMantenimiento solicitudMantenimiento)
        {
            if (ModelState.IsValid)
            {
                db.Entry(solicitudMantenimiento).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            Usuario usu = (Usuario)Session["Usuario"];

            ViewBag.IdChofer = new SelectList(db.Usuario.Where(x => x.IdUsuario == usu.IdUsuario), "IdUsuario", "Nombres");
            ViewBag.IdVehiculo = new SelectList(db.VehiculoChofer.Where(x => x.IdChofer == usu.IdUsuario && x.Activo == true).Select(x => new { idVehiculo = x.Vehiculo.idVehiculo, placa = x.Vehiculo.placaLetras + x.Vehiculo.placaNumeros }), "idVehiculo", "placa");
            return View(solicitudMantenimiento);
        }

        [PermisoAttribute(Accion = "Delete", Controlador = "SolicitudMantenimiento")]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SolicitudMantenimiento solicitudMantenimiento = await db.SolicitudMantenimiento.FindAsync(id);
            if (solicitudMantenimiento == null)
            {
                return HttpNotFound();
            }
            return View(solicitudMantenimiento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            SolicitudMantenimiento solicitudMantenimiento = await db.SolicitudMantenimiento.FindAsync(id);
            //db.SolicitudMantenimiento.Remove(solicitudMantenimiento);
            solicitudMantenimiento.Activo = false;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [PermisoAttribute(Accion = "Aprobar", Controlador = "SolicitudMantenimiento")]
        public async Task<ActionResult> Aprobar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SolicitudMantenimiento solicitudMantenimiento = await db.SolicitudMantenimiento.FindAsync(id);
            if (solicitudMantenimiento == null)
            {
                return HttpNotFound();
            }
            return View(solicitudMantenimiento);
        }

        [HttpPost, ActionName("Aprobar")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AprobarConfirmed(int id)
        {
            SolicitudMantenimiento solicitudMantenimiento = await db.SolicitudMantenimiento.FindAsync(id);
            //db.SolicitudMantenimiento.Remove(solicitudMantenimiento);
            solicitudMantenimiento.Aprobado = true;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [PermisoAttribute(Accion = "Desaprobar", Controlador = "SolicitudMantenimiento")]
        public async Task<ActionResult> Desaprobar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SolicitudMantenimiento solicitudMantenimiento = await db.SolicitudMantenimiento.FindAsync(id);
            if (solicitudMantenimiento == null)
            {
                return HttpNotFound();
            }
            return View(solicitudMantenimiento);
        }

        [HttpPost, ActionName("Desaprobar")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DesaprobarConfirmed(int id)
        {
            SolicitudMantenimiento solicitudMantenimiento = await db.SolicitudMantenimiento.FindAsync(id);
            //db.SolicitudMantenimiento.Remove(solicitudMantenimiento);
            solicitudMantenimiento.Desaprobado = true;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
