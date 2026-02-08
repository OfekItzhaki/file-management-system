# Deployment Guide

This guide covers deploying the Horizon File Management System to production using Render (backend) and Vercel (frontend).

## Architecture Overview

- **Frontend**: React + Vite app deployed to Vercel
- **Backend**: .NET 8 API deployed to Render
- **Database**: PostgreSQL on Render
- **Cache**: Redis on Render
- **Storage**: Cloudinary for file storage

## Prerequisites

1. **Render Account**: Sign up at [render.com](https://render.com)
2. **Vercel Account**: Sign up at [vercel.com](https://vercel.com)
3. **Cloudinary Account**: Sign up at [cloudinary.com](https://cloudinary.com)
4. **Git Repository**: Code pushed to GitHub/GitLab/Bitbucket

## Cost Estimate

- Render Free Tier: PostgreSQL (free), Redis (free), Web Service (free with limitations)
- Vercel Free Tier: Hobby plan (free for personal projects)
- Cloudinary Free Tier: 25 GB storage, 25 GB bandwidth/month
- **Total**: $0/month for free tiers, ~$14/month for production-ready setup

## Step 1: Deploy Backend to Render

### Option A: Using render.yaml (Recommended)

1. **Push code to Git repository**
   ```bash
   git add .
   git commit -m "chore: add deployment configuration"
   git push
   ```

2. **Connect to Render**
   - Go to [Render Dashboard](https://dashboard.render.com)
   - Click "New" → "Blueprint"
   - Connect your Git repository
   - Render will automatically detect `render.yaml`

3. **Configure Environment Variables**
   - In Render dashboard, go to your API service
   - Add the following environment variables:
     - `CloudinarySettings__CloudName`: Your Cloudinary cloud name
     - `CloudinarySettings__ApiKey`: Your Cloudinary API key
     - `CloudinarySettings__ApiSecret`: Your Cloudinary API secret

4. **Wait for deployment**
   - Render will automatically build and deploy all services
   - Note the API URL (e.g., `https://horizon-fms-api.onrender.com`)

### Option B: Manual Setup

1. **Create PostgreSQL Database**
   - Dashboard → New → PostgreSQL
   - Name: `horizon-fms-db`
   - Plan: Free
   - Note the connection string

2. **Create Redis Instance**
   - Dashboard → New → Redis
   - Name: `horizon-fms-redis`
   - Plan: Free
   - Note the connection string

3. **Create Web Service**
   - Dashboard → New → Web Service
   - Connect your Git repository
   - Settings:
     - Name: `horizon-fms-api`
     - Environment: Docker
     - Region: Oregon (or closest to your users)
     - Branch: `main`
     - Dockerfile Path: `./FileManagementSystem.API/Dockerfile`
     - Docker Context: `.`
   - Environment Variables:
     - `ASPNETCORE_ENVIRONMENT`: `Production`
     - `ConnectionStrings__DefaultConnection`: [PostgreSQL connection string]
     - `REDIS_CONNECTION`: [Redis connection string]
     - `CloudinarySettings__CloudName`: [Your Cloudinary cloud name]
     - `CloudinarySettings__ApiKey`: [Your Cloudinary API key]
     - `CloudinarySettings__ApiSecret`: [Your Cloudinary API secret]
     - `CloudinarySettings__IsEnabled`: `true`
     - `Storage__RootPath`: `/app/storage`
     - `ThumbnailSettings__Directory`: `/app/storage/thumbnails`

## Step 2: Deploy Frontend to Vercel

### Option A: Using Vercel CLI (Recommended)

1. **Install Vercel CLI**
   ```bash
   npm install -g vercel
   ```

2. **Login to Vercel**
   ```bash
   vercel login
   ```

3. **Deploy**
   ```bash
   vercel
   ```

4. **Configure Environment Variables**
   - When prompted, add:
     - `VITE_API_URL`: Your Render API URL (e.g., `https://horizon-fms-api.onrender.com/api/v1`)

5. **Deploy to Production**
   ```bash
   vercel --prod
   ```

### Option B: Using Vercel Dashboard

1. **Connect Repository**
   - Go to [Vercel Dashboard](https://vercel.com/dashboard)
   - Click "Add New" → "Project"
   - Import your Git repository

2. **Configure Build Settings**
   - Framework Preset: **Vite** (select from dropdown)
   - Root Directory: `FileManagementSystem.Web` (click Edit button)
   - Build Command: `npm run build` (auto-detected)
   - Output Directory: `dist` (auto-detected)
   - Install Command: `npm install` (auto-detected)

3. **Add Environment Variables**
   - Go to Project Settings → Environment Variables
   - Add:
     - `VITE_API_URL`: Your Render API URL (e.g., `https://horizon-fms-api.onrender.com/api/v1`)

4. **Deploy**
   - Click "Deploy"
   - Wait for deployment to complete
   - Note your frontend URL (e.g., `https://horizon-fms.vercel.app`)

## Step 3: Configure CORS

Update your backend to allow requests from your Vercel domain:

1. **Edit `FileManagementSystem.API/Program.cs`**
   - Find the CORS configuration
   - Add your Vercel domain to allowed origins:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowFrontend", policy =>
       {
           policy.WithOrigins(
               "http://localhost:8080",
               "https://horizon-fms.vercel.app" // Add your Vercel domain
           )
           .AllowAnyMethod()
           .AllowAnyHeader()
           .AllowCredentials();
       });
   });
   ```

2. **Commit and push changes**
   ```bash
   git add .
   git commit -m "chore: add production CORS configuration"
   git push
   ```

3. **Render will auto-deploy** the updated API

## Step 4: Verify Deployment

1. **Check API Health**
   - Visit: `https://your-api-url.onrender.com/health`
   - Should return: `Healthy`

2. **Check Frontend**
   - Visit your Vercel URL
   - Try uploading a file
   - Verify it appears in Cloudinary dashboard

3. **Check Logs**
   - Render: Dashboard → Your Service → Logs
   - Vercel: Dashboard → Your Project → Deployments → View Function Logs

## Troubleshooting

### API Returns 500 Error
- Check Render logs for detailed error messages
- Verify all environment variables are set correctly
- Ensure database connection string is correct

### Frontend Can't Connect to API
- Verify `VITE_API_URL` is set correctly in Vercel
- Check CORS configuration in backend
- Ensure API is running (check Render dashboard)

### Files Not Uploading
- Verify Cloudinary credentials are correct
- Check Cloudinary dashboard for usage limits
- Review API logs for upload errors

### Database Connection Issues
- Verify PostgreSQL is running on Render
- Check connection string format
- Ensure database migrations have run

## Monitoring & Maintenance

### Health Checks
- Render automatically monitors `/health` endpoint
- Set up uptime monitoring (UptimeRobot, Pingdom, etc.)

### Logs
- Render: Real-time logs in dashboard
- Consider adding external logging (Datadog, Loggly, etc.)

### Backups
- Render Free Tier: No automatic backups
- Paid plans: Daily automated backups
- Manual backup: Use `pg_dump` via Render shell

### Updates
- Push to main branch → Render auto-deploys
- Push to main branch → Vercel auto-deploys
- Monitor deployment status in respective dashboards

## Security Checklist

- ✅ HTTPS enabled (automatic on Render & Vercel)
- ✅ Environment variables secured (not in code)
- ✅ CORS configured for production domain only
- ✅ Security headers configured (see `vercel.json`)
- ✅ Database credentials rotated regularly
- ✅ API rate limiting enabled
- ✅ Cloudinary signed uploads (if needed)

## Scaling Considerations

### When to Upgrade from Free Tier

**Render Free Tier Limitations:**
- Web services spin down after 15 minutes of inactivity
- 750 hours/month of runtime
- Limited CPU and memory

**Upgrade to Starter ($7/month) when:**
- You need 24/7 uptime
- Cold starts are affecting UX
- You exceed free tier limits

**Vercel Free Tier Limitations:**
- 100 GB bandwidth/month
- 6,000 build minutes/month

**Upgrade to Pro ($20/month) when:**
- You exceed bandwidth limits
- You need team collaboration features
- You need advanced analytics

### Horizontal Scaling
- Render: Upgrade plan for more instances
- Database: Consider managed PostgreSQL (AWS RDS, Azure Database)
- Cache: Upgrade Redis plan or use Redis Cloud
- Storage: Cloudinary scales automatically

## Alternative Deployment Options

### Backend Alternatives
- **AWS**: ECS/Fargate + RDS + ElastiCache
- **Azure**: App Service + Azure Database + Azure Cache
- **Google Cloud**: Cloud Run + Cloud SQL + Memorystore
- **DigitalOcean**: App Platform + Managed Database

### Frontend Alternatives
- **Netlify**: Similar to Vercel, great for static sites
- **AWS S3 + CloudFront**: More control, requires setup
- **Azure Static Web Apps**: Integrated with Azure services
- **Cloudflare Pages**: Fast global CDN

---

*Last Updated: February 2026*
