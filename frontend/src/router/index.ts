import Vue from 'vue'
import VueRouter, { RouteConfig } from 'vue-router'
import { Manager } from '@/main'
import { HubConnectionState } from '@microsoft/signalr'

Vue.use(VueRouter)

const routes: Array<RouteConfig> = [
	{
		path: '/',
		name: 'Index',
		//component: () => import("../views/Editor.vue"),
	},
	{
		path: '/editor',
		name: 'Editor',
		component: () => import("../views/Editor.vue"),
		beforeEnter: (to, from, next) => {
			if(Manager.connection?.state === HubConnectionState.Connected)
			{
				next();
			}
			else
			{
				next(false);
			}
		}
	},
	{
		path: '/devices',
		name: 'Devices',
		component: () => import("../views/Devices.vue"),
		beforeEnter: (to, from, next) => {
			if(Manager.connection?.state === HubConnectionState.Connected)
			{
				next();
			}
			else
			{
				next(false);
			}
		}
	},
]

const router = new VueRouter({
  mode: 'history',
  base: process.env.BASE_URL,
  routes
})

export default router
