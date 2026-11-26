import pandas as pd, numpy as np, matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
U="./"
c=pd.read_csv(U+"comparacion-20261126-085323.csv")
e=pd.read_csv(U+"estabilidad-20261126-085323.csv")
# Perf
c["grado"]=c["grado"].astype(int)
pv=c.pivot(index="grado",columns="muestras",values=["bernstein_media_ms","casteljau_media_ms","razon_tiempo"])
print(pv.round(3).to_string())
# crossover per muestras
for m in (100,1000,10000):
    s=c[c.muestras==m][["grado","razon_tiempo"]]
    print(m, s[s.razon_tiempo>=1].head(1).values)
# Stability
e["bf"]=e.bernstein_finito=="si"
g=e[e.t>0][e.t<1] if False else e
g=e.groupby("grado").agg(
 bern_media=("error_bernstein","mean"),bern_max=("error_bernstein","max"),
 cast_media=("error_casteljau","mean"),cast_max=("error_casteljau","max"),
 finito=("bf","mean"))
g["ratio_media"]=g.bern_media/g.cast_media
print(g.to_string())
# Interior t only (excluding t=0,1 exact)
ei=e[(e.t>0)&(e.t<1)]
g2=ei.groupby("grado").agg(bern=("error_bernstein","mean"),cast=("error_casteljau","mean"))
g2["ratio"]=g2.bern/g2.cast
print(g2.to_string())
# per t at degree 1000
print(e[e.grado==1000].groupby("t")[["error_bernstein","error_casteljau"]].mean())
g.to_csv("resumen_estabilidad.csv"); pv.to_csv("resumen_rendimiento.csv")
# plots
plt.rcParams.update({"font.size":10})
fig,ax=plt.subplots(1,3,figsize=(14,4))
for a,m in zip(ax,(100,1000,10000)):
    s=c[c.muestras==m]
    a.errorbar(s.grado,s.bernstein_media_ms,s.bernstein_desv_ms,marker="o",label="Bernstein",capsize=2)
    a.errorbar(s.grado,s.casteljau_media_ms,s.casteljau_desv_ms,marker="s",label="De Casteljau",capsize=2)
    a.set_title(f"{m} muestras");a.set_xlabel("Grado");a.set_ylabel("Tiempo medio (ms)");a.grid(alpha=.3);a.legend()
plt.tight_layout();plt.savefig("fig1_tiempo_vs_grado.png",dpi=160);plt.close()
fig,ax=plt.subplots(figsize=(6,4))
for m in (100,1000,10000):
    s=c[c.muestras==m];ax.plot(s.grado,s.razon_tiempo,marker="o",label=f"{m} muestras")
ax.axhline(1,color="k",ls="--");ax.set_xlabel("Grado");ax.set_ylabel("Tiempo Casteljau / Bernstein");ax.set_title("Razón de tiempos (>1: Bernstein más rápido)");ax.grid(alpha=.3);ax.legend()
plt.tight_layout();plt.savefig("fig2_razon_tiempo.png",dpi=160);plt.close()
fig,ax=plt.subplots(figsize=(6.5,4.2))
gg=g.copy()
gb=gg.bern_media.where(gg.finito==1)
ax.semilogy(gg.index,gg.cast_media.replace(0,np.nan),marker="s",label="De Casteljau")
ax.semilogy(gg.index,gb,marker="o",label="Bernstein")
ax.axvline(1100,color="r",ls=":");ax.text(1000,ax.get_ylim()[0]*5,"Bernstein: NaN\nen grado 1100",color="r",ha="right",fontsize=8)
ax.set_xscale("log");ax.set_xlabel("Grado (escala log)");ax.set_ylabel("Error medio vs. referencia (decimal)");ax.set_title("Estabilidad numérica");ax.grid(alpha=.3,which="both");ax.legend()
plt.tight_layout();plt.savefig("fig3_error_vs_grado.png",dpi=160);plt.close()
