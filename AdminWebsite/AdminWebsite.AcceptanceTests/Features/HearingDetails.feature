﻿Feature: Hearing Details Section
	As a Case Admin or VH-Officer
	I need to be able to add hearing details
	So that the correct information is available to all participants who are joining the hearing
	
@VIH-3582 @smoketest
Scenario: Display no dropdown on hearing details page for one case type
	Given Admin user is on microsoft login page
	And Case Admin logs into Vh-Admin website 
	And user is on hearing details page
	Then case type dropdown should not be populated

@VIH-3582 @smoketest
Scenario: Display dropdown on hearing details page for more than one case type
	Given Admin user is on microsoft login page
	And CaseAdminFinRemedyCivilMoneyClaims logs into Vh-Admin website 
	And user is on hearing details page
	Then case type dropdown should be populated